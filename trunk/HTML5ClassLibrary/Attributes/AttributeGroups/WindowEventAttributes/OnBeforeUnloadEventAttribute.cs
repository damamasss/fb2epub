﻿using HTML5ClassLibrary.Attributes.Events;

namespace HTML5ClassLibrary.Attributes.AttributeGroups.WindowEventAttributes
{
    /// <summary>
    /// Script to be run before the document is unloaded
    /// </summary>
    public class OnBeforeUnloadEventAttribute : OnEventAttribute
    {
        #region Overrides of OnEventAttribute

        protected override string GetAttributeName()
        {
            return "onbeforeunload";
        }

        #endregion

    }
}