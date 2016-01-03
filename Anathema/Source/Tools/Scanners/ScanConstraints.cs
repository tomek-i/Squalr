﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anathema
{
    /// <summary>
    /// Class for storing a collection of constraints to be used in a scan that applies more than one constraint per update
    /// </summary>
    class ScanConstraints
    {
        private List<ValueConstraintsItem> ValueConstraints;
        private Boolean FilterScientificNotation;

        public ScanConstraints()
        {
            FilterScientificNotation = false;
            ValueConstraints = new List<ValueConstraintsItem>();
        }

        public void AddConstraint(ValueConstraintsItem ValueConstraintsItem)
        {
            this.ValueConstraints.Add(ValueConstraintsItem);
        }

        public void RemoveConstraint(Int32 Index)
        {

        }

        public void SetFilterScientificNotation(Boolean FilterScientificNotation)
        {
            this.FilterScientificNotation = FilterScientificNotation;
        }
        
    } // End class

} // End namespace