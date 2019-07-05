using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WolfReleaser.General
{
    public interface IParserMatch<T> : INotifyPropertyChanged
    {
        bool IsEnabled { get; set; }
        bool IsMatch(string line);
        object Process(string line, T target);
    }

    public abstract class BaseParserMatch<T> : IParserMatch<T>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected readonly object pass = new object();

        protected bool m_enabled = true;
        protected const StringComparison CMP = StringComparison.OrdinalIgnoreCase;

        public bool IsEnabled
        {
            get => this.m_enabled;
            set
            {
                if (value != this.m_enabled)
                {
                    this.m_enabled = value;
                    this.PropertyChanged?.Invoke(
                        this,
                        new PropertyChangedEventArgs(nameof(IsEnabled)));
                }
            }
        }

        public abstract bool IsMatch(string line);
        public abstract object Process(string line, T target);
    }
}
