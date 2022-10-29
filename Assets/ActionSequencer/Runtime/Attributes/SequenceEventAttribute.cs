using System;

namespace ActionSequencer
{
    /// <summary>
    /// SequenceEventにつけられるAttribute
    /// </summary>
    public class SequenceEventAttribute : Attribute
    {
        public string DisplayName { get; private set; }
        
        public SequenceEventAttribute(string displayName)
        {
            DisplayName = displayName;
        }
    }
}
