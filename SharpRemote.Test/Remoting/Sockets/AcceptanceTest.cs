﻿using System;
using System.Collections.Generic;
using System.Net;
using NUnit.Framework;

namespace SharpRemote.Test.Remoting.Sockets
{
	[TestFixture]
	public class AcceptanceTest
		: AbstractAcceptanceTest
	{
		protected override void Connect(IRemotingEndPoint client, IRemotingEndPoint server)
		{
			((SocketRemotingEndPointClient)client).Connect(
				((SocketRemotingEndPointServer)server).LocalEndPoint,
				TimeSpan.FromMinutes(1));
		}

		protected override IEnumerable<IServant> Servants(IRemotingEndPoint client)
		{
			return ((SocketRemotingEndPointClient) client).Servants;
		}

		protected override IRemotingEndPoint CreateClient()
		{
			return new SocketRemotingEndPointClient("Client");
		}

		protected override IRemotingEndPoint CreateServer()
		{
			return new SocketRemotingEndPointServer("Server");
		}

		protected override void Bind(IRemotingEndPoint server)
		{
			((SocketRemotingEndPointServer)server).Bind(IPAddress.Loopback);
		}
	}
}