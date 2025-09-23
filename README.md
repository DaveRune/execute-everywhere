# Execute Everywhere

Perform your custom action in all prefabs and or scenes in a Unity project.  
Use this when you have an issue across a project that requires you to find every instance of something, and make a change to it.  
This was created due to the need to add and configure a new component to all text components when adding support for a new locale.

## Example Use
 - Create an Action (Scriptable Object)
 - Configure it to perform your task (Write C#)
 - Open "Tools > Execute Everywhere"
 - Define the Component to find all usages of
 - Define the Action to run
 - Search
 - Remove any specific results from search
 - Run action

## Create an Action
- Create a new C# script inside a folder called 'Editor'
- Derive from ExecutionAction or ExecutionActionOnComponent
- Add a CreateAssetMenu attribute
- Override one of the following methods:
  - ExecuteActionOnComponent(MonoBehaviour component)
  - ExecuteActionOnContainingPrefab(GameObject prefab)
- Write your action in C#
- Save
- Right-click in your 'Editor' folder 

## Planned Future Features
 - Run on all assets of a certain type
