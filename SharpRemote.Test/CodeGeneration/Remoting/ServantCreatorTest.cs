﻿using System;
using System.IO;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using SharpRemote.CodeGeneration;
using SharpRemote.Test.Types.Interfaces;
using SharpRemote.Test.Types.Interfaces.PrimitiveTypes;

namespace SharpRemote.Test.CodeGeneration.Remoting
{
	[TestFixture]
	public sealed class ServantCreatorTest
	{
		private ServantCreator _creator;
		private Random _random;
		private ulong _objectId;
		private Mock<IEndPointChannel> _channel;

		[SetUp]
		public void SetUp()
		{
			var seed = (int)(DateTime.Now.Ticks % Int32.MaxValue);
			Console.WriteLine("Seed: {0}", seed);
			_random = new Random(seed);
			_channel = new Mock<IEndPointChannel>();
			_creator = new ServantCreator(_channel.Object);
		}

		private IServant TestGenerate<T>(T subject)
		{
			Type type = null;
			new Action(() => type = _creator.GenerateSubject<T>())
				.ShouldNotThrow();

			type.Should().NotBeNull();

			_objectId = (ulong)_random.Next();
			var servant = _creator.CreateServant(_objectId, subject);
			servant.Should().NotBeNull();
			servant.ObjectId.Should().Be(_objectId);
			servant.Subject.Should().BeSameAs(subject);

			return servant;
		}

		[Test]
		[Description("Verifies that raising an event on the subject causes the servant to serialize the arguments and send the call via IEndPointChannel.CallRemoteMethod")]
		public void TestEventInt32()
		{
			var subject = new Mock<IEventInt32>();
			var servant = TestGenerate(subject.Object);

			var callRemoteMethodInvoked = false;
			_channel.Setup(x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
					.Callback((ulong id, string methodName, MemoryStream stream) =>
						{
							id.Should().Be(servant.ObjectId);
							methodName.Should().Be("Foobar");
							stream.Should().NotBeNull();
							var reader = new BinaryReader(stream);
							reader.ReadInt32().Should().Be(42);
							callRemoteMethodInvoked = true;
						});
			subject.Raise(x => x.Foobar += null, 42);
			callRemoteMethodInvoked.Should().BeTrue();
		}

		[Test]
		public void TestEmpty()
		{
			var subject = new Mock<IEmpty>();
			var servant = TestGenerate(subject.Object);
		}

		[Test]
		public void TestGetProperty()
		{
			var subject = new Mock<IGetDoubleProperty>();
			var servant = TestGenerate(subject.Object);

			subject.Setup(x => x.Value).Returns(Math.PI);

			var outStream = new MemoryStream();
			var @out = new BinaryWriter(outStream);
			servant.InvokeMethod("get_Value", null, @out);

			outStream.Position = 0;
			var reader = new BinaryReader(outStream);
			reader.ReadDouble().Should().BeApproximately(Math.PI, 0);
		}
	}
}