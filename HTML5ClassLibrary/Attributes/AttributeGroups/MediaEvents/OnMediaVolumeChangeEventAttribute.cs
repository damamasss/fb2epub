﻿using HTML5ClassLibrary.Attributes.Events;

namespace HTML5ClassLibrary.Attributes.AttributeGroups.MediaEvents
{
    /// <summary>
    /// Script to be run each time the volume is changed which (includes setting the volume to "mute")
    /// </summary>
    public class OnMediaVolumeChangeEventAttribute : OnEventAttribute
    {
        #region Overrides of OnEventAttribute

        protected override string GetAttributeName()
        {
            return "onvolumechange";
        }

        #endregion
    }
}
