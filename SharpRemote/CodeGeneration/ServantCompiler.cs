﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using SharpRemote.CodeGeneration.Serialization;

namespace SharpRemote.CodeGeneration
{
	public sealed class ServantCompiler
		: Compiler
	{
		private readonly AssemblyBuilder _assembly;
		private readonly Type _interfaceType;
		private readonly ModuleBuilder _module;
		private readonly string _moduleName;
		private readonly TypeBuilder _typeBuilder;
		private readonly FieldBuilder _subject;
		private readonly List<KeyValuePair<EventInfo, MethodInfo>> _eventInvocationMethods;

		public ServantCompiler(Serializer serializer,
		                       AssemblyName assemblyName,
		                       string subjectTypeName,
		                       Type interfaceType)
			: base(serializer)
		{
			_interfaceType = interfaceType;
			_assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
			_moduleName = assemblyName.Name + ".dll";
			_module = _assembly.DefineDynamicModule(_moduleName);

			_typeBuilder = _module.DefineType(subjectTypeName, TypeAttributes.Class, typeof(object));
			_typeBuilder.AddInterfaceImplementation(typeof(IServant));

			_eventInvocationMethods = new List<KeyValuePair<EventInfo, MethodInfo>>();

			_subject = _typeBuilder.DefineField("_subject", interfaceType, FieldAttributes.Private | FieldAttributes.InitOnly);
			ObjectId = _typeBuilder.DefineField("_objectId", typeof(ulong), FieldAttributes.Private | FieldAttributes.InitOnly);
			Channel = _typeBuilder.DefineField("_channel", typeof (IEndPointChannel),
			                                   FieldAttributes.Private | FieldAttributes.InitOnly);
			Serializer = _typeBuilder.DefineField("_serializer", typeof(ISerializer),
												FieldAttributes.Private | FieldAttributes.InitOnly);
		}

		private void GenerateGetSerializer()
		{
			var method = _typeBuilder.DefineMethod("get_Serializer", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final, typeof(ISerializer), null);
			var gen = method.GetILGenerator();

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldfld, Serializer);
			gen.Emit(OpCodes.Ret);

			_typeBuilder.DefineMethodOverride(method, Methods.GrainGetSerializer);
		}

		private void GenerateGetObjectId()
		{
			var method = _typeBuilder.DefineMethod("get_Id", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final, typeof(ulong), null);
			var gen = method.GetILGenerator();

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldfld, ObjectId);
			gen.Emit(OpCodes.Ret);

			_typeBuilder.DefineMethodOverride(method, Methods.GrainGetObjectId);
		}

		public Type Generate()
		{
			GenerateGetObjectId();
			GenerateGetSerializer();
			GenerateGetSubject();
			GenerateDispatchMethod();
			GenerateEvents();
			GenerateCtor();

			var proxyType = _typeBuilder.CreateType();
			return proxyType;
		}

		private void GenerateEvents()
		{
			// For every event we have to compile a method that essentially does the same that the proxy compiler
			// does for interface methods: serialize the arguments into a stream and then call IEndPointChannel.InvokeMethod
			var allEvents = _interfaceType.GetEvents();
			foreach (var @event in allEvents)
			{
				GenerateEvent(@event);
			}
		}

		private void GenerateEvent(EventInfo @event)
		{
			var delegateType = @event.EventHandlerType;
			var methodInfo = delegateType.GetMethod("Invoke");

			var methodName = string.Format("On{0}", @event.Name);
			var parameterTypes = methodInfo.GetParameters().Select(x => x.ParameterType).ToArray();
			var returnType = typeof (void);
			var method = _typeBuilder.DefineMethod(methodName,
												   MethodAttributes.Public,
													returnType,
													parameterTypes);

			GenerateMethodInvocation(method, @event.Name, parameterTypes, returnType);

			_eventInvocationMethods.Add(new KeyValuePair<EventInfo, MethodInfo>(@event, method));
		}

		private void GenerateGetSubject()
		{
			var method = _typeBuilder.DefineMethod("get_Subject",
			                                       MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final,
			                                       typeof (object),
			                                       null);

			var gen = method.GetILGenerator();
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldfld, _subject);
			gen.Emit(OpCodes.Ret);

			_typeBuilder.DefineMethodOverride(method, Methods.ServantGetSubject);
		}

		private void GenerateDispatchMethod()
		{
			var method = _typeBuilder.DefineMethod("InvokeMethod",
			                                       MethodAttributes.Public | MethodAttributes.Virtual,
												   typeof(void),
												   new[]
													   {
														   typeof(string),
														   typeof(BinaryReader),
														   typeof(BinaryWriter)
													   });

			var gen = method.GetILGenerator();

			var name = gen.DeclareLocal(typeof (string));
			var @throw = gen.DefineLabel();
			var @ret = gen.DefineLabel();

			// if (method == null) goto ret
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Stloc, name);
			gen.Emit(OpCodes.Ldloc, name);
			gen.Emit(OpCodes.Brfalse, @throw);

			var allMethods = _interfaceType
				.GetMethods(BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public)
				.Concat(_interfaceType.GetInterfaces().SelectMany(x => x.GetMethods(BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public)))
				.OrderBy(x => x.Name)
				.ToArray();
			var labels = new Label[allMethods.Length];
			int index = 0;
			foreach (var methodInfo in allMethods)
			{
				gen.Emit(OpCodes.Ldloc, name);
				gen.Emit(OpCodes.Ldstr, methodInfo.Name);
				gen.Emit(OpCodes.Call, Methods.StringEquality);

				var @true = gen.DefineLabel();
				labels[index++] = @true;
				gen.Emit(OpCodes.Brtrue, @true);
			}

			gen.Emit(OpCodes.Br, @throw);

			for (int i = 0; i < allMethods.Length; ++i)
			{
				var methodInfo = allMethods[i];
				var label = labels[i];

				gen.MarkLabel(label);
				ExtractArgumentsAndCallMethod(gen, methodInfo);
				gen.Emit(OpCodes.Br, @ret);
			}

			gen.MarkLabel(@throw);
			gen.Emit(OpCodes.Ldstr, "Method '{0}' not found");
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Call, Methods.StringFormat);
			gen.Emit(OpCodes.Newobj, Methods.ArgumentExceptionCtor);
			gen.Emit(OpCodes.Throw);

			gen.MarkLabel(@ret);
			gen.Emit(OpCodes.Ldarg_3);
			gen.Emit(OpCodes.Call, Methods.BinaryWriterFlush);
			gen.Emit(OpCodes.Ret);

			_typeBuilder.DefineMethodOverride(method, Methods.ServantInvokeMethod);
		}

		private new void ExtractArgumentsAndCallMethod(ILGenerator gen, MethodInfo methodInfo)
		{
			if (methodInfo.ReturnType != typeof(void))
				gen.Emit(OpCodes.Ldarg_3);

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldfld, _subject);
			base.ExtractArgumentsAndCallMethod(gen, methodInfo);
		}

		private void GenerateCtor()
		{
			var builder = _typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new[]
				{
					typeof (ulong),
					typeof (IEndPointChannel),
					typeof (ISerializer),
					_interfaceType
				});

			var gen = builder.GetILGenerator();
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, Methods.ObjectCtor);

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Stfld, ObjectId);

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_2);
			gen.Emit(OpCodes.Stfld, Channel);

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_3);
			gen.Emit(OpCodes.Stfld, Serializer);

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg, 4);
			gen.Emit(OpCodes.Stfld, _subject);

			foreach (var pair in _eventInvocationMethods)
			{
				var eventAddMethod = pair.Key.AddMethod;
				var delegateType = pair.Key.EventHandlerType;
				var onEventMethod = pair.Value;

				AddOnFireEvent(gen, eventAddMethod, delegateType, onEventMethod);
			}

			gen.Emit(OpCodes.Ret);
		}

		private void AddOnFireEvent(ILGenerator gen, MethodInfo eventAddMethod, Type delegateType, MethodInfo onEventMethod)
		{
			// We need to find the constructor of the Action/Delegate that we're creating....
			var ctor = delegateType.GetConstructor(new[] {typeof (object), typeof (IntPtr)});
			if (ctor == null)
				throw new NotImplementedException(string.Format("Could not find a suitable constructor for delegate '{0}' with an (object, IntPtr) signature", delegateType));

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldfld, _subject);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldftn, onEventMethod);
			gen.Emit(OpCodes.Newobj, ctor);
			gen.Emit(OpCodes.Callvirt, eventAddMethod);
		}

		public void Save()
		{
			_assembly.Save(_moduleName);
		}
	}
}