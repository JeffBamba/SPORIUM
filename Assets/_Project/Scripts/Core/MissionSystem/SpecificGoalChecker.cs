using System;

namespace _Project.Sporae.Core
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SpecificGoalCheckerAttribute : Attribute
    {
        public Type SpellType;
    }
}