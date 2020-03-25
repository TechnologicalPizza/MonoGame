﻿// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Eto.Forms;

namespace MonoGame.Tools.Pipeline
{
    partial class ReferenceDialog : Dialog<bool>
    {
        protected class RefItem
        {
            public string Assembly { get; set; }
            public string Location { get; set; }

            public RefItem(string assembly, string location)
            {
                Assembly = assembly;
                Location = location;
            }
        }

        public List<string> References { get; private set; }

        private IController _controller;
        private FileFilter _dllFileFilter, _allFileFilter;
        private SelectableFilterCollection<RefItem> _dataStore;

        public ReferenceDialog(IController controller, IEnumerable<string> refs)
        {
            InitializeComponent();

            _controller = controller;

            _dllFileFilter = new FileFilter("Dll Files (*.dll)", new[] { ".dll" });
            _allFileFilter = new FileFilter("All Files (*.*)", new[] { ".*" });

            var assemblyColumn = new GridColumn
            {
                HeaderText = "Assembly",
                DataCell = new TextBoxCell("Assembly"),
                Sortable = true
            };
            grid1.Columns.Add(assemblyColumn);

            var locationColumn = new GridColumn
            {
                HeaderText = "Location",
                DataCell = new TextBoxCell("Location"),
                Sortable = true
            };
            grid1.Columns.Add(locationColumn);

            grid1.DataStore = _dataStore = new SelectableFilterCollection<RefItem>(grid1);

            foreach (var rf in refs)
                _dataStore.Add(new RefItem(Path.GetFileName(rf), _controller.GetFullPath(rf)));
        }

        public override void Close()
        {
            References = new List<string>();

            foreach (var item in _dataStore)
                References.Add(_controller.GetRelativePath(item.Location));
            base.Close();
        }

        private void Grid1_SelectionChanged(object sender, EventArgs e)
        {
            buttonRemove.Enabled = grid1.SelectedItems.Count() > 0;
        }

        private void Grid1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Keys.Delete)
                ButtonRemove_Click(sender, e);
        }

        private void ButtonAdd_Click(object sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Directory = new Uri(_controller.ProjectItem.Location),
                MultiSelect = true
            };
            dialog.Filters.Add(_dllFileFilter);
            dialog.Filters.Add(_allFileFilter);
            dialog.CurrentFilter = _dllFileFilter;

            if (dialog.ShowDialog(this) == DialogResult.Ok)
                foreach (var fileName in dialog.Filenames)
                    _dataStore.Add(new RefItem(Path.GetFileName(fileName), fileName));
        }

        private void ButtonRemove_Click(object sender, EventArgs e)
        {
            foreach (var item in grid1.SelectedItems)
                _dataStore.Remove(item as RefItem);
        }

        private void ButtonOk_Click(object sender, EventArgs e)
        {
            Result = true;
            Close();
        }

        private void ButtonCancel_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}