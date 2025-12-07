using MalbersAnimations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Weapons")]
    [SerializeField] private MWeaponManager weaponManager;
    [SerializeField] private HolsterID webSnareRightHand;
    private void OnEnable()
    {
        if (!weaponManager)
            weaponManager = GetComponent<MWeaponManager>();
        UIButtonActions.OnWebSnareBtnEnable += TakeWeapon;
        UIButtonActions.OnWebSnareStart += ShootSnareStart;
        UIButtonActions.OnWebSnareFinish += ShootSnareFinished;

        KopkariMainUI.OnWebSnareBtnEnable += TakeWeapon;
        KopkariMainUI.OnWebSnareStart += ShootSnareStart;
        KopkariMainUI.OnWebSnareFinish += ShootSnareFinished;


    }
    private void OnDestroy()
    {
        UIButtonActions.OnWebSnareBtnEnable -= TakeWeapon;
        UIButtonActions.OnWebSnareStart -= ShootSnareStart;
        UIButtonActions.OnWebSnareFinish -= ShootSnareFinished;

        KopkariMainUI.OnWebSnareBtnEnable -= TakeWeapon;
        KopkariMainUI.OnWebSnareStart -= ShootSnareStart;
        KopkariMainUI.OnWebSnareFinish -= ShootSnareFinished;
    }

    #region Web Snare(Tur otish)
    public void TakeWeapon()
    {
        weaponManager.Holster_Equip(webSnareRightHand);
        Debug.Log("Calling me");
    }
    public void ShootSnareFinished()
    {
        weaponManager.MainAttack(false);
    }
    private void ShootSnareStart()
    {
        weaponManager.MainAttack(true);
    }
    #endregion

}
