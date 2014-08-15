﻿using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using HTMLClassLibrary.Exceptions;

namespace HTMLClassLibrary.BaseElements.InlineElements.TextBasedElements
{
    public abstract class TextBasedElement : HTMLItem, IInlineItem
    {
        protected override bool IsValidSubType(IHTMLItem item)
        {
            if (item is SimpleHTML5Text)
            {
                return item.IsValid();
            }
            if (item is IInlineItem)
            {
                return item.IsValid();
            }
            return false;
        }


        /// <summary>
        /// Checks it element data is valid
        /// </summary>
        /// <returns>
        /// true if valid
        /// </returns>
        public override bool IsValid()
        {
            return true;
        }
    }
}
