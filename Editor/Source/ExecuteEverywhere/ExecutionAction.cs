using UnityEditor;
using UnityEngine;

namespace KnightForge.Editor.ExecuteEverywhere
{
    // Example CreateAssetMenu for derived classes:
    // [CreateAssetMenu(fileName = "My Execution Action", menuName = "Execute Everywhere/My Execution Action")]
    public abstract class ExecutionAction : ScriptableObject
    {
        public enum ContextType
        {
            Scene,
            Prefab
        }

        protected ContextType Context { get; private set; }

        public void Execute(Object context)
        {
            switch (context)
            {
                case MonoBehaviour component: ExecuteActionOnComponent(component); break;
                case GameObject prefab: ExecuteActionOnContainingPrefab(prefab); break;
            }
        }

        public void SetActiveContext(ContextType context) =>
            Context = context;

        protected virtual void ExecuteActionOnContainingPrefab(GameObject prefab)
        {
        }

        protected virtual void ExecuteActionOnComponent(MonoBehaviour component)
        {
        }
    }

    public abstract class ExecutionActionOnComponent<T> : ExecutionAction where T : MonoBehaviour
    {
        protected override void ExecuteActionOnComponent(MonoBehaviour component)
        {
            if (component is T typedComponent)
                ExecuteActionOnComponent(typedComponent);
        }

        protected abstract void ExecuteActionOnComponent(T component);
    }
}