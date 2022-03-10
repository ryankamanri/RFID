using System;
using System.Collections.Generic;
using System.Text;

namespace RFID.Environments
{
	public enum TagState
	{
		ReadyState,
		ArbitrateState,
		ReplyState,
		AcknowledgedState,
		OpenState,
		SecuredState,
		KilledState
	}
}
