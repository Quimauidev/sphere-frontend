using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Sphere.Common.Constans
{
    public class NoSpecialCharBehavior : Behavior<Entry>
    {
        protected override void OnAttachedTo(Entry entry)
        {
            base.OnAttachedTo(entry);
            entry.TextChanged += OnTextChanged;
        }

        protected override void OnDetachingFrom(Entry entry)
        {
            base.OnDetachingFrom(entry);
            entry.TextChanged -= OnTextChanged;
        }

        private void OnTextChanged(object? sender, TextChangedEventArgs e)
        {
            if (sender is not Entry entry) return;

            // Chỉ cho phép chữ cái có dấu tiếng Việt + khoảng trắng
            string allowedPattern = @"[^a-zA-ZÀ-ỹ0-9\s]";
            if (Regex.IsMatch(e.NewTextValue, allowedPattern))
            {
                string cleaned = Regex.Replace(e.NewTextValue, allowedPattern, "");
                entry.Text = cleaned;
            }
        }
    }
}
