#### [ChensClassicItems](index 'index')
### [Chen.ClassicItems.Items.Common](Chen_ClassicItems_Items_Common 'Chen.ClassicItems.Items.Common')
## MortarTube Class
Singleton item class powered by TILER2 that implements Mortar Tube functionality.  
```csharp
public class MortarTube : TILER2.Item<Chen.ClassicItems.Items.Common.MortarTube>
```

Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; [TILER2.AutoConfigContainer](https://docs.microsoft.com/en-us/dotnet/api/TILER2.AutoConfigContainer 'TILER2.AutoConfigContainer') &#129106; [TILER2.T2Module](https://docs.microsoft.com/en-us/dotnet/api/TILER2.T2Module 'TILER2.T2Module') &#129106; [TILER2.CatalogBoilerplate](https://docs.microsoft.com/en-us/dotnet/api/TILER2.CatalogBoilerplate 'TILER2.CatalogBoilerplate') &#129106; [TILER2.Item](https://docs.microsoft.com/en-us/dotnet/api/TILER2.Item 'TILER2.Item') &#129106; [TILER2.Item&lt;](https://docs.microsoft.com/en-us/dotnet/api/TILER2.Item-1 'TILER2.Item`1')[MortarTube](Chen_ClassicItems_Items_Common_MortarTube 'Chen.ClassicItems.Items.Common.MortarTube')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/TILER2.Item-1 'TILER2.Item`1') &#129106; MortarTube  

| Properties | |
| :--- | :--- |
| [mortarPrefab](Chen_ClassicItems_Items_Common_MortarTube_mortarPrefab 'Chen.ClassicItems.Items.Common.MortarTube.mortarPrefab') | Contains the mortar projectile prefab. Must invoke SetupMortarProjectile() for it to be initialized.<br/> |

| Methods | |
| :--- | :--- |
| [SetupMortarProjectile()](Chen_ClassicItems_Items_Common_MortarTube_SetupMortarProjectile() 'Chen.ClassicItems.Items.Common.MortarTube.SetupMortarProjectile()') | Sets up the mortar projectile. Always invoke the method if one needs to borrow the mortar prefab.<br/> |
