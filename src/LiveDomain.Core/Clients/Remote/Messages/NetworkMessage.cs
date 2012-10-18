﻿using System;
using System.Collections.Generic;

namespace LiveDomain.Core
{
    [Serializable]
    public class NetworkMessage
    {
		public bool RequiresAcknowledgement { get; set; }
        public object Payload { get; set; }
		public Exception Error { get; set; }
        public bool Succeeded
        {
            get
            {
                return Error == null;
            }
        }
    }

    [Serializable]
    public class RedirectMessage : NetworkMessage
    {
        public string Host { get; set; }
        public int Port { get; set; }
    }
    


    [Serializable]
    public class Heartbeat : NetworkMessage
    {
    }

	[Serializable]
	public class TransitioningMessage : NetworkMessage
	{
		public TransitioningMessage(int waitTime)
		{
			Payload = waitTime;
		}

		public int WaitTime
		{
			get { return (int)Payload; }
		}
	}

}
