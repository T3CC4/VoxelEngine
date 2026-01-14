using OpenTK.Mathematics;

namespace VoxelEngine.Game;

public class DayNightCycle
{
    private float currentTime = 0.25f; // Start at dawn (0=midnight, 0.5=noon, 1=midnight)
    private float dayLength = 300.0f; // 5 minutes for a full day/night cycle

    public float DayLength
    {
        get => dayLength;
        set => dayLength = Math.Max(10.0f, value); // Minimum 10 seconds
    }

    public float CurrentTime => currentTime;

    // 0-1 where 0=midnight, 0.25=dawn, 0.5=noon, 0.75=dusk, 1=midnight
    public float DayNightValue => currentTime;

    public bool IsDaytime => GetSunHeight() > 0;
    public bool IsNighttime => !IsDaytime;

    public void Update(float deltaTime)
    {
        currentTime += deltaTime / dayLength;
        if (currentTime >= 1.0f)
        {
            currentTime -= 1.0f;
        }
    }

    public void SetTime(float time)
    {
        currentTime = Math.Clamp(time, 0.0f, 1.0f);
    }

    public float GetSunHeight()
    {
        // Returns -1 to 1, where 1 is noon, -1 is midnight
        return MathF.Sin(currentTime * MathF.PI * 2.0f);
    }

    public Vector3 GetSunDirection()
    {
        // Sun moves in an arc across the sky
        float angle = currentTime * MathF.PI * 2.0f;
        float x = MathF.Cos(angle) * 0.3f;
        float y = -MathF.Sin(angle); // Negative because we want sun high at noon
        float z = MathF.Sin(angle) * 0.3f;

        return Vector3.Normalize(new Vector3(x, y, z));
    }

    public Vector3 GetMoonDirection()
    {
        // Moon is opposite to the sun
        return -GetSunDirection();
    }

    public Vector3 GetSkyColor()
    {
        float sunHeight = GetSunHeight();

        Vector3 dayColor = new Vector3(0.53f, 0.81f, 0.92f);
        Vector3 sunsetColor = new Vector3(0.9f, 0.6f, 0.4f);
        Vector3 nightColor = new Vector3(0.05f, 0.05f, 0.15f);

        if (sunHeight > 0.0f)
        {
            // Daytime
            if (sunHeight > 0.8f)
            {
                return dayColor;
            }
            else
            {
                return Vector3.Lerp(sunsetColor, dayColor, (sunHeight - 0.0f) / 0.8f);
            }
        }
        else
        {
            // Nighttime
            if (sunHeight < -0.8f)
            {
                return nightColor;
            }
            else
            {
                return Vector3.Lerp(nightColor, sunsetColor, (sunHeight + 0.8f) / 0.8f);
            }
        }
    }

    public string GetTimeOfDay()
    {
        if (currentTime < 0.2f || currentTime > 0.8f)
            return "Night";
        else if (currentTime < 0.3f)
            return "Dawn";
        else if (currentTime > 0.7f)
            return "Dusk";
        else
            return "Day";
    }

    public void SetToNoon()
    {
        currentTime = 0.5f;
    }

    public void SetToMidnight()
    {
        currentTime = 0.0f;
    }

    public void SetToDawn()
    {
        currentTime = 0.25f;
    }

    public void SetToDusk()
    {
        currentTime = 0.75f;
    }
}
