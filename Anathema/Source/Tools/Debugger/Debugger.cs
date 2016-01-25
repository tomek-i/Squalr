﻿using Binarysharp.MemoryManagement;
using Binarysharp.MemoryManagement.Memory;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Anathema
{
    /// <summary>
    /// Handles the displaying of results
    /// </summary>
    class Debugger : IDebuggerModel, IProcessObserver
    {
        private MemorySharp MemoryEditor;
        private Snapshot Snapshot;

        private Type ScanType;

        private Int32 StartReadIndex;
        private Int32 EndReadIndex;
        private ConcurrentDictionary<Int32, String> IndexValueMap;
        private Boolean ForceRefreshFlag;

        public Debugger()
        {
            InitializeProcessObserver();
            SetScanType(typeof(Int32));
            ForceRefreshFlag = false;
            IndexValueMap = new ConcurrentDictionary<Int32, String>();
            Begin();
        }

        ~Debugger()
        {
            End();
        }

        public void InitializeProcessObserver()
        {
            ProcessSelector.GetInstance().Subscribe(this);
        }

        public void UpdateMemoryEditor(MemorySharp MemoryEditor)
        {
            this.MemoryEditor = MemoryEditor;
        }

        public void ForceRefresh()
        {
            ForceRefreshFlag = true;
        }

        public void EnableResults()
        {
            OnEventEnableDebugger(new DebuggerEventArgs());
            Begin();
        }

        public void DisableResults()
        {
            CancelFlag = true;
            OnEventDisableDebugger(new DebuggerEventArgs());
        }

        public override void SetScanType(Type ScanType)
        {
            this.ScanType = ScanType;
        }

        public override Type GetScanType()
        {
            return ScanType;
        }

        public override void UpdateReadBounds(Int32 StartReadIndex, Int32 EndReadIndex)
        {
            this.StartReadIndex = StartReadIndex;
            this.EndReadIndex = EndReadIndex;
        }

        public override void Begin()
        {
            base.Begin();
        }

        protected override void Update()
        {
            base.Update();

            Snapshot ActiveSnapshot = SnapshotManager.GetInstance().GetActiveSnapshot();
            if (ForceRefreshFlag || Snapshot != ActiveSnapshot)
            {
                ForceRefreshFlag = false;
                Snapshot = ActiveSnapshot;
                
                // Send the size of the filtered memory to the display
                DebuggerEventArgs Args = new DebuggerEventArgs();
                Args.ElementCount = (Snapshot == null ? 0 : Snapshot.GetElementCount());
                Args.MemorySize = (Snapshot == null ? 0 : Snapshot.GetMemorySize());
                OnEventFlushCache(Args);
                return;
            }

            if (Snapshot == null)
                return;

            IndexValueMap.Clear();

            Int32 ElementCount = (Int32)(Math.Min((UInt64)Int32.MaxValue, Snapshot.GetElementCount()));

            for (Int32 Index = StartReadIndex; Index < EndReadIndex; Index++)
            {
                if (Index < 0 || Index >= ElementCount)
                    continue;

                Boolean ReadSuccess;
                String Value = MemoryEditor.Read(ScanType, Snapshot[Index].BaseAddress, out ReadSuccess, false).ToString();

                IndexValueMap[Index] = Value;
            }

            OnEventReadValues(new DebuggerEventArgs());
        }

        public override void AddSelectionToTable(Int32 Index)
        {
            Snapshot ActiveSnapshot = SnapshotManager.GetInstance().GetActiveSnapshot();

            if (ActiveSnapshot == null)
                return;

            Table.GetInstance().AddTableItem((UInt64)ActiveSnapshot[Index].BaseAddress, ScanType);
        }

        public override IntPtr GetAddressAtIndex(Int32 Index)
        {
            if (Snapshot == null || Index >= (Int32)Snapshot.GetElementCount())
                return IntPtr.Zero;

            return Snapshot[Index].BaseAddress;
        }

        public override String GetValueAtIndex(Int32 Index)
        {
            if (IndexValueMap.ContainsKey(Index))
                return IndexValueMap[Index];

            return "-";
        }

        public override String GetLabelAtIndex(Int32 Index)
        {
            if (Snapshot == null || Index >= (Int32)Snapshot.GetElementCount())
                return "-";

            dynamic Label = String.Empty;
            if (((dynamic)Snapshot)[Index].ElementLabel != null)
                Label = ((dynamic)Snapshot)[Index].ElementLabel;

            return Label.ToString();
        }

    } // End class

} // End namespace