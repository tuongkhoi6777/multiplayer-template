using Core;
using UnityEngine;

namespace GamePlay
{
    public enum ShootMode
    {
        Auto, // full auto
        Burst, // 3 shoot per fire
        Single, // 1 shoot per fire
        Length
    }
    
    public class Weapon
    {
        public string Name { get; }
        public int MainAmmo { get; set; }
        public int SubAmmo { get; set; }
        public int AmmoCapacity { get; }
        public int Damage { get; }
        public float FireRate { get; }
        public float ReloadTime { get; }
        public float VerticalRecoil { get; }
        public float HorizontalRecoil { get; }
        public float RecoilRecovery { get; }

        public Weapon(string name, int mainAmmo, int subAmmo, int damage, float fireRate, float reloadTime, float verticalRecoil, float horizontalRecoil, float recoilRecovery)
        {
            Name = name;
            MainAmmo = mainAmmo;
            SubAmmo = subAmmo;
            AmmoCapacity = mainAmmo;
            Damage = damage;
            FireRate = fireRate;
            ReloadTime = reloadTime;
            VerticalRecoil = verticalRecoil;
            HorizontalRecoil = horizontalRecoil;
            RecoilRecovery = recoilRecovery;
        }
    }

    public class WeaponController : MonoBehaviour
    {
        public Weapon weapon = GameDefine.AK;
        public PlayerController player;
        public GameObject bulletPrefab;
        public Vector3 offset = new(0, 0, 0);
        public ShootMode shootMode = ShootMode.Auto;
        public float nextFireTime = 0f;

        void OnEnable()
        {
            // add player fire event listener
            EventManager.emitter.On(EventManager.PLAYER_FIRE, Fire);
        }

        void OnDisable()
        {
            // remove player fire event listener
            EventManager.emitter.Off(EventManager.PLAYER_FIRE);
        }

        void Update()
        {
            RecoilRecover();
        }

        // call init when player buy a new gun
        void Init(Weapon w, PlayerController p)
        {
            weapon = w;
            player = p;
        }

        public void DropWeapon()
        {
            player = null;
        }

        public void TakeWeapon(PlayerController p)
        {
            player = p;
        }

        public void SwitchShootingMode()
        {
            // switch to between shooting mode
            if (++shootMode >= ShootMode.Length)
            {
                shootMode = 0;
            }
        }

        public void Fire()
        {
            if (Time.time > nextFireTime && player)
            {
                int shootMultiply = shootMode == ShootMode.Burst ? 3 : 1;
                nextFireTime = Time.time + (shootMode == ShootMode.Auto ? 1f / weapon.FireRate : 0.5f);

                for (int i = 0; i < shootMultiply; i++)
                {
                    Shoot();
                }
            }
        }

        public void Shoot()
        {
            if (weapon.MainAmmo > 0)
            {
                weapon.MainAmmo--;

                Camera camera = player.PlayerCamera;
                Vector3 position = camera.transform.position;
                Vector3 forward = camera.transform.forward + offset;

                GameObject bullet = Instantiate(bulletPrefab, position, Quaternion.LookRotation(forward));
                bullet.GetComponent<Bullet>().Init(forward.normalized, weapon.Damage);

                ApplyRecoil();
            }
        }

        public void Reload()
        {
            if (weapon.MainAmmo < weapon.AmmoCapacity && weapon.SubAmmo > 0)
            {
                int ammoToReload = Mathf.Min(weapon.SubAmmo, weapon.AmmoCapacity - weapon.MainAmmo);
                weapon.MainAmmo += ammoToReload;
                weapon.SubAmmo -= ammoToReload;

                nextFireTime = Time.time + weapon.ReloadTime;
            }
        }

        private void ApplyRecoil()
        {
            // Horizontal recoil
            float x = Random.Range(-weapon.HorizontalRecoil, weapon.HorizontalRecoil);

            // Vertical recoil
            float y = Random.Range(weapon.VerticalRecoil * 0.5f, weapon.VerticalRecoil);

            // Apply recoil to the offset based on the weapon's recoil properties
            offset += new Vector3(x, y, 0f);
        }

        private void RecoilRecover()
        {
            offset = Vector3.Lerp(offset, Vector3.zero, weapon.RecoilRecovery * Time.deltaTime);
        }
    }
}