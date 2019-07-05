﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WolfReleaser.General
{
    public interface IParser<T> : INotifyPropertyChanged
    {
        bool IsEnabled { get; }
        string FullPath { get; }
        string FileName { get; }
        IEnumerable<string> Lines { get; }
        T Parse();
    }

    public abstract class BaseParser<T> : IParser<T>
    {
        public abstract event PropertyChangedEventHandler PropertyChanged;

        protected string filepath;
        protected string[] lines;

        public abstract bool IsEnabled { get; }

        public IEnumerable<string> Lines => this.lines;
        public string FullPath => this.filepath;

        public string FileName => Path.GetFileNameWithoutExtension(this.filepath);

        public abstract T Parse();
    }
}
