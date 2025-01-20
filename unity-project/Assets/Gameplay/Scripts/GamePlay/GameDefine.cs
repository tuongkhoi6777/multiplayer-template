namespace GamePlay
{
    public class GameDefine
    {
        public static Weapon AK => new(
            name: "AK-47",
            mainAmmo: 30,
            subAmmo: 90,
            damage: 27,
            fireRate: 9.5f,
            reloadTime: 2.0f,
            verticalRecoil: 1.5f,
            horizontalRecoil: 0.5f,
            recoilRecovery: 2f
        );
    }
}