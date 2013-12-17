using System;
using System.Collections.Generic;
using Jypeli;

class ClimbingPlatformCharacter : PlatformCharacter
{
    public ClimbingPlatformCharacter(double width, double height)
        : base(width, height)
    {
        hangs = false;
        Climbing = false;
    }

    private bool hangs;

    public bool Climbing { get; private set; }
    public bool Hangs
    {
        get
        {
            return hangs;
        }
        set
        {
            if (value)
            {
                PlayAnimation(AnimHangs, RestartHangsAnim);
                StaticFriction = 1000000000000000000.0;
                Stop();
            }
            else if (hangs && !value)
            {
                StaticFriction = 0.4;
                IgnoresGravity = false;
                Reset();
            }
            hangs = value;
        }
    }

    public Animation AnimClimb { get; set; }
    public Animation AnimHangs { get; set; }

    public bool LoopClimbAnim { get; set; }

    public void Climb(double dx, double dy, double speed)
    {
        //IgnoresCollisionResponse = true;
        Climbing = true;
        Hit(new Vector(-dx / 10, dy * speed));
        PlayAnimation(AnimClimb, RestartClimbAnim);
    }

    private void RestartClimbAnim()
    {
        if (LoopClimbAnim && Climbing)
            PlayAnimation(AnimClimb, RestartClimbAnim);
        else
            Reset(); // Reset animations
    }
    private void RestartHangsAnim()
    {
        if (Hangs)
            PlayAnimation(AnimHangs, RestartHangsAnim);
        else
            Reset(); // Reset animations
    }

    private void stopClimb()
    {
        IgnoresCollisionResponse = false;
        // Has reached
        Climbing = false;
    }
}
