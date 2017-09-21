﻿namespace Microsoft.ApplicationInsights.Extensibility.Implementation.External
{
    using System;
    using System.Diagnostics;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;

    /// <summary>
    /// Partial class to add the EventData attribute and any additional customizations to the generated type.
    /// </summary>
#if !NET45
    // .Net 4.5 has a custom implementation of RichPayloadEventSource
    [System.Diagnostics.Tracing.EventData(Name = "PartB_AvailabilityData")]
#endif
    internal partial class MessageData
    {
        public MessageData DeepClone()
        {
            var other = new MessageData();
            other.ver = this.ver;            
            other.message = this.message;
            other.severityLevel = this.severityLevel;
            Debug.Assert(other.properties != null, "The constructor should have allocated properties dictionary");
            Utils.CopyDictionary(this.properties, other.properties);
            return other;
        }
    }
}