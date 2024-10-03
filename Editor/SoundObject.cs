namespace BobuEditor
{
    using UnityEngine;

    //Müzikler için kullanýlan alan
    [CreateAssetMenu(fileName = "NewSound", menuName = "Sound Library/Sound")]
    public class SoundObject : ScriptableObject
    {
        public string soundName;
        public string downloadLink;
        public SoundTag tag;        // Artýk bir liste deðil, tek seçim
        public SubSoundTag subTag;  // Alt tür
    }

    public enum SoundTag
    {
        All,
        Ambient,
        Music,
        SFX,
        Voice
    }

    public enum SubSoundTag
    {
        None,           // Eðer alt tür kullanýlmayacaksa
        AI,
        Agriculture,
        Animal,
        Button,
        CartoonMinionFussin,
        CharacterMove,
        Home,
        MotorVehicle,
        OverallSound,
        Piano,
        Sport,
        Water,
        Weather,
        AnimalsInfo,
        BigMax,
        Colors,
        Pasha,
        Pika,
        Rose,
        Toys
    } 
}