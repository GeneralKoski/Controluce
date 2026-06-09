namespace Controluce.Level;

// Qualcosa che una WeightPlate (o altro trigger) può attivare/disattivare.
public interface IActivatable
{
    void SetActivated(bool active);
}
