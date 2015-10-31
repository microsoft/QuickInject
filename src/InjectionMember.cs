namespace QuickInject
{
    using System.Linq.Expressions;

    public abstract class InjectionMember
    {
        public Expression Factory { get; protected set; }
    }
}