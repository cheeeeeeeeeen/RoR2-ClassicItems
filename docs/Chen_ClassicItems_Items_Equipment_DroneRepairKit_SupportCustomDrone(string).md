#### [ChensClassicItems](index 'index')
### [Chen.ClassicItems.Items.Equipment](Chen_ClassicItems_Items_Equipment 'Chen.ClassicItems.Items.Equipment').[DroneRepairKit](Chen_ClassicItems_Items_Equipment_DroneRepairKit 'Chen.ClassicItems.Items.Equipment.DroneRepairKit')
## DroneRepairKit.SupportCustomDrone(string) Method
Adds a support for a custom drone so that Drone Repair Kit also heals and applies regen to them.  
```csharp
public bool SupportCustomDrone(string masterName);
```
#### Parameters
<a name='Chen_ClassicItems_Items_Equipment_DroneRepairKit_SupportCustomDrone(string)_masterName'></a>
`masterName` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
The CharacterMaster name of the drone.
  
#### Returns
[System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')  
True if the drone is supported. False if it is already supported.
