﻿using System.Reflection.Emit;

namespace SharpRemote.CodeGeneration.Serialization.Binary
{
	internal sealed class BinaryReadValueMethodCompiler
		: AbstractReadValueMethodCompiler
	{
		public BinaryReadValueMethodCompiler(CompilationContext context) : base(context)
		{
		}

		protected override void EmitEndReadProperty(ILGenerator gen, PropertyDescription property)
		{
			throw new System.NotImplementedException();
		}

		protected override void EmitBeginRead(ILGenerator gen)
		{
		}

		protected override void EmitEndRead(ILGenerator gen)
		{
		}

		protected override void EmitReadByte(ILGenerator gen)
		{
			throw new System.NotImplementedException();
		}

		protected override void EmitReadSByte(ILGenerator gen)
		{
			throw new System.NotImplementedException();
		}

		protected override void EmitReadUInt16(ILGenerator gen)
		{
			throw new System.NotImplementedException();
		}

		protected override void EmitReadInt16(ILGenerator gen)
		{
			throw new System.NotImplementedException();
		}

		protected override void EmitReadUInt32(ILGenerator gen)
		{
			throw new System.NotImplementedException();
		}

		protected override void EmitReadInt32(ILGenerator gen)
		{
			throw new System.NotImplementedException();
		}

		protected override void EmitReadUInt64(ILGenerator gen)
		{
			throw new System.NotImplementedException();
		}

		protected override void EmitReadInt64(ILGenerator gen)
		{
			throw new System.NotImplementedException();
		}

		protected override void EmitReadDecimal(ILGenerator gen)
		{
			throw new System.NotImplementedException();
		}

		protected override void EmitReadFloat(ILGenerator gen)
		{
			throw new System.NotImplementedException();
		}

		protected override void EmitReadDouble(ILGenerator gen)
		{
			throw new System.NotImplementedException();
		}

		protected override void EmitReadString(ILGenerator gen)
		{
			throw new System.NotImplementedException();
		}

		protected override void EmitBeginReadField(ILGenerator gen, FieldDescription field)
		{
			throw new System.NotImplementedException();
		}

		protected override void EmitEndReadField(ILGenerator gen, FieldDescription field)
		{
			throw new System.NotImplementedException();
		}

		protected override void EmitBeginReadProperty(ILGenerator gen, PropertyDescription property)
		{
			throw new System.NotImplementedException();
		}

		protected override void EmitReadHintAndGrainId(ILGenerator generator)
		{
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Callvirt, Methods.ReadByte);
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Callvirt, Methods.ReadLong);
		}
	}
}