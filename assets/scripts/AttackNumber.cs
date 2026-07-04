// Which attack animation a gun drives on its wielder. The member names match the
// AnimatedSprite2D animation names (e.g. Fire1 -> the "Fire1" animation), so a boss
// with several attacks can show a distinct animation per weapon. Default falls back
// to the generic "Fire"/"Shoot" animation.
public enum AttackNumber
{
    Default = 0,
    Fire1 = 1,
    Fire2 = 2,
    Fire3 = 3,
    Fire4 = 4,
    Fire5 = 5,
    Fire6 = 6,
}
