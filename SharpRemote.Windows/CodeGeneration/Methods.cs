﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace SharpRemote.CodeGeneration
{
	public static class Methods
	{
		public static readonly MethodInfo ChannelCallRemoteMethod;
		public static readonly MethodInfo ReadDouble;
		public static readonly MethodInfo GrainGetObjectId;
		public static readonly MethodInfo GrainGetSerializer;
		public static readonly ConstructorInfo ObjectCtor;
		public static readonly ConstructorInfo MemoryStreamCtor;
		public static readonly ConstructorInfo BinaryWriterCtor;
		public static readonly MethodInfo WriteBytes;
		public static readonly MethodInfo WriteString;
		public static readonly MethodInfo BinaryWriterFlush;
		public static readonly ConstructorInfo BinaryReaderCtor;
		public static readonly MethodInfo WriteDouble;
		public static readonly MethodInfo WriteSingle;
		public static readonly MethodInfo WriteUInt;
		public static readonly MethodInfo WriteInt32;
		public static readonly MethodInfo WriteULong;
		public static readonly MethodInfo WriteLong;
		public static readonly MethodInfo WriteUShort;
		public static readonly MethodInfo WriteShort;
		public static readonly MethodInfo WriteByte;
		public static readonly MethodInfo WriteSByte;
		public static readonly MethodInfo WriteBool;
		public static readonly MethodInfo WriteObject;
		public static readonly MethodInfo StreamSetPosition;
		public static readonly MethodInfo ObjectGetType;
		public static readonly FieldInfo StringEmpty;
		public static readonly MethodInfo GrainInvoke;
		public static readonly MethodInfo StringEquality;
		public static readonly MethodInfo ReadBytes;
		public static readonly MethodInfo ReadString;
		public static readonly MethodInfo ReadSingle;
		public static readonly MethodInfo ReadLong;
		public static readonly MethodInfo ReadULong;
		public static readonly MethodInfo ReadInt32;
		public static readonly MethodInfo ReadUInt;
		public static readonly MethodInfo ReadShort;
		public static readonly MethodInfo ReadUShort;
		public static readonly MethodInfo ReadSByte;
		public static readonly MethodInfo ReadByte;
		public static readonly MethodInfo ReadBool;
		public static readonly ConstructorInfo NotImplementedCtor;
		public static readonly ConstructorInfo ArgumentExceptionCtor;
		public static readonly MethodInfo ServantGetSubject;
		public static readonly MethodInfo StringFormat;
		public static readonly MethodInfo ArrayGetLength;
		public static readonly MethodInfo DelegateCombine;
		public static readonly MethodInfo DelegateRemove;
		public static readonly MethodInfo InterlockedCompareExchangeGeneric;
		public static readonly MethodInfo SerializerWriteObject;
		public static readonly MethodInfo SerializerReadObject;
		public static readonly MethodInfo CreateTypeFromName;
		public static readonly MethodInfo RemotingEndPointGetOrCreateServant;
		public static readonly MethodInfo RemotingEndPointGetOrCreateProxy;

		static Methods()
		{
			MemoryStreamCtor = typeof (MemoryStream).GetConstructor(new Type[0]);
			StreamSetPosition = typeof (MemoryStream).GetProperty("Position").GetSetMethod();

			BinaryWriterCtor = typeof(BinaryWriter).GetConstructor(new[] { typeof(Stream) });
			BinaryWriterFlush = typeof(BinaryWriter).GetMethod("Flush");
			BinaryReaderCtor = typeof (BinaryReader).GetConstructor(new[] {typeof (Stream)});

			GrainGetObjectId = typeof(IGrain).GetMethod("get_ObjectId");
			GrainGetSerializer = typeof(IGrain).GetMethod("get_Serializer");
			ServantGetSubject = typeof (IServant).GetMethod("get_Subject");
			GrainInvoke = typeof (IGrain).GetMethod("Invoke");

			ObjectCtor = typeof(object).GetConstructor(new Type[0]);
			ChannelCallRemoteMethod = typeof(IEndPointChannel).GetMethod("CallRemoteMethod");

			ReadBytes = typeof (BinaryReader).GetMethod("ReadBytes");
			ReadString = typeof(BinaryReader).GetMethod("ReadString");
			ReadDouble = typeof(BinaryReader).GetMethod("ReadDouble");
			ReadSingle = typeof(BinaryReader).GetMethod("ReadSingle");
			ReadLong = typeof(BinaryReader).GetMethod("ReadInt64");
			ReadULong = typeof(BinaryReader).GetMethod("ReadUInt64");
			ReadInt32 = typeof(BinaryReader).GetMethod("ReadInt32");
			ReadUInt = typeof(BinaryReader).GetMethod("ReadUInt32");
			ReadShort = typeof(BinaryReader).GetMethod("ReadInt16");
			ReadUShort = typeof(BinaryReader).GetMethod("ReadUInt16");
			ReadSByte = typeof(BinaryReader).GetMethod("ReadSByte");
			ReadByte = typeof(BinaryReader).GetMethod("ReadByte");
			ReadBool = typeof(BinaryReader).GetMethod("ReadBoolean");

			WriteBytes = typeof (BinaryWriter).GetMethod("Write", new[] {typeof (byte[])});
			WriteString = typeof(BinaryWriter).GetMethod("Write", new[]{typeof(string)});
			WriteDouble = typeof(BinaryWriter).GetMethod("Write", new[]{typeof(double)});
			WriteSingle = typeof(BinaryWriter).GetMethod("Write", new[]{typeof(Single)});
			WriteLong = typeof(BinaryWriter).GetMethod("Write", new[]{typeof(long)});
			WriteULong = typeof(BinaryWriter).GetMethod("Write", new[] { typeof(ulong) });
			WriteInt32 = typeof(BinaryWriter).GetMethod("Write", new[] { typeof(int) });
			WriteUInt = typeof(BinaryWriter).GetMethod("Write", new[] { typeof(uint) });
			WriteShort = typeof(BinaryWriter).GetMethod("Write", new[] { typeof(short) });
			WriteUShort = typeof(BinaryWriter).GetMethod("Write", new[] { typeof(ushort) });
			WriteSByte = typeof(BinaryWriter).GetMethod("Write", new[] { typeof(sbyte) });
			WriteByte = typeof(BinaryWriter).GetMethod("Write", new[] { typeof(byte) });
			WriteBool = typeof(BinaryWriter).GetMethod("Write", new[] { typeof(bool) });
			WriteObject = typeof (ISerializer).GetMethod("WriteObject",
			                                             new[] {typeof (BinaryWriter), typeof (object)});

			ObjectGetType = typeof (object).GetMethod("GetType");

			StringEmpty = typeof (string).GetField("Empty", BindingFlags.Public | BindingFlags.Static);
			StringEquality = typeof (string).GetMethod("op_Equality", new[] {typeof (string), typeof (string)});
			StringFormat = typeof (string).GetMethod("Format", new[]{typeof(string), typeof(object)});

			NotImplementedCtor = typeof (NotImplementedException).GetConstructor(new Type[0]);
			ArgumentExceptionCtor = typeof (ArgumentException).GetConstructor(new[] {typeof (string)});

			ArrayGetLength = typeof (byte[]).GetProperty("Length").GetMethod;

			DelegateCombine = typeof (Delegate).GetMethod("Combine", new[]{typeof(Delegate), typeof(Delegate)});
			DelegateRemove = typeof (Delegate).GetMethod("Remove", new[] {typeof (Delegate), typeof (Delegate)});

			InterlockedCompareExchangeGeneric =
				typeof (Interlocked).GetMethods().First(x => x.Name == "CompareExchange" && x.IsGenericMethod);

			SerializerWriteObject = typeof (ISerializer).GetMethod("WriteObject", new[]{typeof(BinaryWriter), typeof(object)});
			SerializerReadObject = typeof(ISerializer).GetMethod("ReadObject", new[] { typeof(BinaryReader) });

			RemotingEndPointGetOrCreateServant = typeof (IRemotingEndPoint).GetMethod("GetExistingOrCreateNewServant");
			RemotingEndPointGetOrCreateProxy = typeof (IRemotingEndPoint).GetMethod("GetExistingOrCreateNewProxy");

			CreateTypeFromName = typeof(Methods).GetMethod("GetType", new[] { typeof(string) });
		}

		public static Type GetType(string name)
		{
			return Type.GetType(name);
		}
	}
}