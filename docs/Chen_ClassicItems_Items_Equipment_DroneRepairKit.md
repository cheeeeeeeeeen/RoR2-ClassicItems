#### [ChensClassicItems](index 'index')
### [Chen.ClassicItems.Items.Equipment](Chen_ClassicItems_Items_Equipment 'Chen.ClassicItems.Items.Equipment')
## DroneRepairKit Class
Singleton equipment class powered by TILER2 that implements Drone Repair Kit functionality.  
```csharp
public class DroneRepairKit : TILER2.Equipment<Chen.ClassicItems.Items.Equipment.DroneRepairKit>
```

Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; [TILER2.AutoConfigContainer](https://docs.microsoft.com/en-us/dotnet/api/TILER2.AutoConfigContainer 'TILER2.AutoConfigContainer') &#129106; [TILER2.T2Module](https://docs.microsoft.com/en-us/dotnet/api/TILER2.T2Module 'TILER2.T2Module') &#129106; [TILER2.CatalogBoilerplate](https://docs.microsoft.com/en-us/dotnet/api/TILER2.CatalogBoilerplate 'TILER2.CatalogBoilerplate') &#129106; [TILER2.Equipment](https://docs.microsoft.com/en-us/dotnet/api/TILER2.Equipment 'TILER2.Equipment') &#129106; [TILER2.Equipment&lt;](https://docs.microsoft.com/en-us/dotnet/api/TILER2.Equipment-1 'TILER2.Equipment`1')[DroneRepairKit](Chen_ClassicItems_Items_Equipment_DroneRepairKit 'Chen.ClassicItems.Items.Equipment.DroneRepairKit')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/TILER2.Equipment-1 'TILER2.Equipment`1') &#129106; DroneRepairKit  

| Properties | |
| :--- | :--- |
| [regenBuff](Chen_ClassicItems_Items_Equipment_DroneRepairKit_regenBuff 'Chen.ClassicItems.Items.Equipment.DroneRepairKit.regenBuff') | The regen buff associated with the Drone Repair Kit to be given to affected drones.<br/> |

| Methods | |
| :--- | :--- |
| [SupportCustomDrone(string)](Chen_ClassicItems_Items_Equipment_DroneRepairKit_SupportCustomDrone(string) 'Chen.ClassicItems.Items.Equipment.DroneRepairKit.SupportCustomDrone(string)') | Adds a support for a custom drone so that Drone Repair Kit also heals and applies regen to them.<br/> |
| [UnsupportCustomDrone(string)](Chen_ClassicItems_Items_Equipment_DroneRepairKit_UnsupportCustomDrone(string) 'Chen.ClassicItems.Items.Equipment.DroneRepairKit.UnsupportCustomDrone(string)') | Removes support for a custom drone, thus removing them from Drone Repair Kit's scope.<br/> |
