using System;
using System.Linq;
using FlaUI.Core.AutomationElements;

namespace PublishToBilibili.Services
{
    public class UiPathFinder
    {
        private readonly AutomationElement _root;

        public UiPathFinder(AutomationElement root)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
        }

        public T Find<T>(params Func<AutomationElement, bool>[] conditions) where T : AutomationElement
        {
            var element = Find(conditions);
            return element?.As<T>();
        }

        public AutomationElement Find(params Func<AutomationElement, bool>[] conditions)
        {
            if (conditions == null || conditions.Length == 0)
            {
                return _root;
            }

            return conditions.Aggregate(_root, (current, condition) => FindNext(current, condition));
        }

        private AutomationElement FindNext(AutomationElement parent, Func<AutomationElement, bool> condition)
        {
            if (parent == null)
            {
                return null;
            }

            var children = parent.FindAllChildren();
            return children.FirstOrDefault(condition);
        }

        public AutomationElement FindChild(Func<AutomationElement, bool> condition)
        {
            if (_root == null)
            {
                return null;
            }

            var children = _root.FindAllChildren();
            return children.FirstOrDefault(condition);
        }

        public T FindChild<T>(Func<AutomationElement, bool> condition) where T : AutomationElement
        {
            var element = FindChild(condition);
            return element?.As<T>();
        }

        public AutomationElement FindDescendant(Func<AutomationElement, bool> condition)
        {
            if (_root == null)
            {
                return null;
            }

            var descendants = _root.FindAllDescendants();
            return descendants.FirstOrDefault(condition);
        }

        public T FindDescendant<T>(Func<AutomationElement, bool> condition) where T : AutomationElement
        {
            var element = FindDescendant(condition);
            return element?.As<T>();
        }

        public T FindWithSibling<T>(Func<AutomationElement, bool> firstCondition, Func<AutomationElement, bool> siblingCondition, Func<AutomationElement, bool> targetCondition) where T : AutomationElement
        {
            var first = FindDescendant(firstCondition);
            if (first == null)
            {
                return null;
            }

            var siblings = first.Parent?.FindAllChildren() ?? Array.Empty<AutomationElement>();
            var sibling = siblings.FirstOrDefault(siblingCondition);
            if (sibling == null)
            {
                return null;
            }

            var children = sibling.FindAllChildren();
            var target = children.FirstOrDefault(targetCondition);
            
            return target?.As<T>();
        }

        public T FindSibling<T>(Func<AutomationElement, bool> referenceCondition, Func<AutomationElement, bool> targetCondition) where T : AutomationElement
        {
            var reference = FindDescendant(referenceCondition);
            if (reference == null)
            {
                return null;
            }

            var siblings = reference.Parent?.FindAllChildren() ?? Array.Empty<AutomationElement>();
            var target = siblings.FirstOrDefault(targetCondition);
            
            return target?.As<T>();
        }
    }
}
