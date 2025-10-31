using System.Threading.Tasks;
using UnityEngine;
using MalbersAnimations.HAP;
using MalbersAnimations;
using MalbersAnimations.Controller;
using MalbersAnimations.Utilities;

public class HorseDataManager : MonoBehaviour
{
    public GameObject horsePrefab;
    private GameObject horseInstance;
    public Transform spawnPoint;

    public Mount CurrentMount { get; private set; }
    public MAnimal CurrentAnimal { get; private set; }
    public MaterialChanger MaterialChanger { get; private set; }

    public KopkariHorseBomb currentBomb { get; private set; }
    public async Task<Mount> SpawnHorseAsync()
    {
        //GameObject horseGO = await AddressablesManager.Instance.LoadAndInstantiateCachedAsync(
        //    "Horse",
        //    position: spawnPoint.position,
        //    rotation: spawnPoint.rotation,
        //    parent: spawnPoint.transform
        //); ;
        horseInstance = Instantiate(horsePrefab, spawnPoint.position, spawnPoint.rotation, spawnPoint.transform);

        //// 4. Ichidan HorseSkinLoader scriptni topamiz
        HorseSkinLoader horseSkinLoader = horseInstance.GetComponentInChildren<HorseSkinLoader>();
        if (horseSkinLoader != null)
        {
            await horseSkinLoader.ApplySkins();
        }
        else
        {
            Debug.Log("❌ HorseSkinLoader component not found on instantiated horse.");
        }
        await Task.Yield(); // Wait 1 frame

        // Wait until Mount component and its MountPoint are ready
        Mount mount = null;
        Transform mountPoint = null;

        while (true)
        {
            mount = horseInstance.GetComponentInChildren<Mount>();
            if (mount != null)
            {
                mountPoint = mount.MountPoint;
                if (mountPoint != null)
                    break;
            }
            await Task.Yield(); // wait another frame if not ready
        }

        CurrentMount = mount;
        CurrentAnimal = horseInstance.GetComponentInChildren<MAnimal>();
        MaterialChanger = horseInstance.GetComponentInChildren<MaterialChanger>();

        //Bomb pyhsics ni olish for use
        currentBomb = horseInstance.GetComponent<KopkariHorseBomb>();
        
        return CurrentMount;
    }
    //Buttonga ulash kerak
    public void OnBombButtonClicked()
    {
        if (currentBomb != null)
        {
            currentBomb.ActivateHere();
        }
        else
        {
            Debug.LogWarning("No player horse bomb bound yet.");
        }
    }
    public void CustomizeHorse(int materialIndex)
    {
        MaterialChanger?.SetAllMaterials(materialIndex);
    }
}
