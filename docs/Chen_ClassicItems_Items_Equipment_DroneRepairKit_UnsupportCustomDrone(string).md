#### [ChensClassicItems](index 'index')
### [Chen.ClassicItems.Items.Equipment](Chen_ClassicItems_Items_Equipment 'Chen.ClassicItems.Items.Equipment').[DroneRepairKit](Chen_ClassicItems_Items_Equipment_DroneRepairKit 'Chen.ClassicItems.Items.Equipment.DroneRepairKit')
## DroneRepairKit.UnsupportCustomDrone(string) Method
Removes support for a custom drone, thus removing them from Drone Repair Kit's scope.  
```csharp
public bool UnsupportCustomDrone(string masterName);
```
#### Parameters
<a name='Chen_ClassicItems_Items_Equipment_DroneRepairKit_UnsupportCustomDrone(string)_masterName'></a>
`masterName` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
The CharacterMaster name of the drone.
  
#### Returns
[System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')  
True if the drone is unsupported. False if it is already unsupported.
