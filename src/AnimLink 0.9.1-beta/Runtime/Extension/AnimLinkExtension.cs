namespace AnimLink
{
    using System;
    using System.Collections.Generic;

    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Provides global extension methods for the AnimLink animation Plugin.
    /// <br>
    /// This static class contains extension methods and helper functions
    /// that extend Unity components (such as <see cref="Transform"/>, 
    /// <see cref="SpriteRenderer"/>, <see cref="CanvasGroup"/> etc.)
    /// to directly create and control AnimLink animations.
    /// </br>
    /// <br>
    /// Example:
    /// <code>
    /// transform.DoPosition(new Vector3(0, 5, 0), 1f)
    ///          .SetEase(Ease.OutQuad)
    ///          .Play();
    /// </code>
    /// </br>
    /// </summary>
    static public class AnimLinkExtension
    {
        //参数顺序: component, target, duration, [other parameters]...
        #region DoPath

        /// <summary>
        /// Creates a <see cref="AnimLink.DoPath"/> animation for the given Transform along a specified path.
        /// </summary>
        /// <param name="transform">The Transform to animate.</param>
        /// <param name="path">An array of Vector3 points defining the path.</param>
        /// <param name="duration">The duration of the animation in seconds.</param>
        /// <returns>A configured <see cref="AnimLink.DoPath"/> instance ready for playback.</returns>
        /// <remarks>
        /// This method is an extension for Transform, allowing for chainable syntax like:
        /// <code>
        /// transform.DoPath(pathPoints, 3f).SetLoops(2).Play();
        /// </code>
        /// </remarks>
        static public DoPath DoPath(this Transform transform, Vector3[] path, float duration)
        {
            return new DoPath().ConfigDoPath(transform, path, duration);
        }

        #endregion
        #region DoPosition

        /// <summary>
        /// Creates a <see cref="AnimLink.DoPosition"/> animation for the given Transform along the specified axis.
        /// </summary>
        /// <param name="transform">The Transform to animate.</param>
        /// <param name="target">The target position value along the specified axis.</param>
        /// <param name="duration">The duration of the animation in seconds.</param>
        /// <param name="axis">The axis along which to animate (X, Y, Z).</param>
        /// <returns>A configured <see cref="AnimLink.DoPosition"/> instance ready for playback.</returns>
        static public DoPosition DoPosition(this Transform transform, float target, float duration, Axis axis)
        {
            return new DoPosition().ConfigDoPos(transform, target, duration, axis); ;
        }

        /// <summary>
        /// Creates a <see cref="AnimLink.DoPosition"/> animation for the given Transform to a specific 3D position.
        /// </summary>
        /// <param name="transform">The Transform to animate.</param>
        /// <param name="position">The target position in world space.</param>
        /// <param name="duration">The duration of the animation in seconds.</param>
        /// <returns>A configured <see cref="AnimLink.DoPosition"/> instance ready for playback.</returns>
        static public DoPosition DoPosition(this Transform transform, Vector3 position, float duration)
        {
            return new DoPosition().ConfigDoPos(transform, position, duration);
        }

        #endregion
        #region DoScale

        /// <summary>
        /// Creates a <see cref="AnimLink.DoScale"/> animation for the given Transform along the specified axis.
        /// </summary>
        /// <param name="transform">The Transform to animate.</param>
        /// <param name="target">The target scale value along the specified axis.</param>
        /// <param name="duration">The duration of the animation in seconds.</param>
        /// <param name="axis">The axis along which to scale (X, Y, Z).</param>
        /// <returns>A configured <see cref="AnimLink.DoScale"/> instance ready for playback.</returns>
        static public DoScale DoScale(this Transform transform, float target, float duration, Axis axis)
        {
            return new DoScale().ConfigDoScale(transform, target, duration, axis);
        }

        /// <summary>
        /// Creates a <see cref="AnimLink.DoScale"/> animation for the given Transform to a specific local scale.
        /// </summary>
        /// <param name="transform">The Transform to animate.</param>
        /// <param name="localScale">The target local scale in 3D space.</param>
        /// <param name="duration">The duration of the animation in seconds.</param>
        /// <returns>A configured <see cref="AnimLink.DoScale"/> instance ready for playback.</returns>
        static public DoScale DoScale(this Transform transform, Vector3 localScale, float duration)
        {
            return new DoScale().ConfigDoScale(transform, localScale, duration);
        }

        #endregion
        #region DoRotation

        /// <summary>
        /// Creates a <see cref="AnimLink.DoRotation"/> animation for the given Transform to rotate to the specified Euler angles.
        /// </summary>
        /// <param name="transform">The Transform to animate.</param>
        /// <param name="eulerAngles">The target rotation in Euler angles (degrees).</param>
        /// <param name="duration">The duration of the animation in seconds.</param>
        /// <returns>A configured <see cref="AnimLink.DoRotation"/> instance ready for playback.</returns>
        static public DoRotation DoRotation(this Transform transform, Vector3 eulerAngles, float duration)
        {
            return new DoRotation().ConfigDoRotation(transform, eulerAngles, duration);
        }

        #endregion
        #region DoAlpha

        /// <summary>
        /// Creates a <see cref="AnimLink.DoAlpha"/> animation for a <see cref="SpriteRenderer"/>.
        /// </summary>
        /// <param name="spriteRenderer">The SpriteRenderer to animate.</param>
        /// <param name="alpha">The target alpha value, between 0 and 1.</param>
        /// <param name="duration">The duration of the animation in seconds.</param>
        /// <returns>A configured <see cref="AnimLink.DoAlpha"/> instance ready for playback.</returns>
        static public DoAlpha DoAlpha(this SpriteRenderer spriteRenderer, float alpha, float duration)
        {
            return new DoAlpha().ConfigDoAlpha(spriteRenderer, alpha, duration, AlphaCompType.Sprite);
        }

        /// <summary>
        /// Creates a <see cref="AnimLink.DoAlpha"/> animation for a <see cref="CanvasGroup"/>.
        /// </summary>
        /// <param name="canvasGroup">The CanvasGroup to animate.</param>
        /// <param name="alpha">The target alpha value, between 0 and 1.</param>
        /// <param name="duration">The duration of the animation in seconds.</param>
        /// <returns>A configured <see cref="AnimLink.DoAlpha"/> instance ready for playback.</returns>
        static public DoAlpha DoAlpha(this CanvasGroup canvasGroup, float alpha, float duration)
        {
            return new DoAlpha().ConfigDoAlpha(canvasGroup, alpha, duration, AlphaCompType.CanvasGroup);
        }

        /// <summary>
        /// Creates a <see cref="AnimLink.DoAlpha"/> animation for a <see cref="Graphic"/> (UI element).
        /// </summary>
        /// <param name="graphic">The Graphic to animate.</param>
        /// <param name="alpha">The target alpha value, between 0 and 1.</param>
        /// <param name="duration">The duration of the animation in seconds.</param>
        /// <returns>A configured <see cref="AnimLink.DoAlpha"/> instance ready for playback.</returns>
        static public DoAlpha DoAlpha(this Graphic graphic, float alpha, float duration)
        {
            return new DoAlpha().ConfigDoAlpha(graphic, alpha, duration, AlphaCompType.CanvasGroup);
        }

        /// <summary>
        /// Creates a <see cref="AnimLink.DoAlpha"/> animation for a <see cref="Material"/>.
        /// </summary>
        /// <param name="material">The Material to animate.</param>
        /// <param name="alpha">The target alpha value, between 0 and 1.</param>
        /// <param name="duration">The duration of the animation in seconds.</param>
        /// <returns>A configured <see cref="AnimLink.DoAlpha"/> instance ready for playback.</returns>
        static public DoAlpha DoAlpha(this Material material, float alpha, float duration)
        {
            return new DoAlpha().ConfigDoAlpha(material, alpha, duration, AlphaCompType.Material);
        }

        /// <summary>
        /// Creates a <see cref="AnimLink.DoAlpha"/> animation for a <see cref="MeshRenderer"/>.
        /// <para>Note: This uses MaterialPropertyBlock. To reset properties, use <see cref="MaterialPropertyCleaner.Clear(Name_)"/>.</para>
        /// </summary>
        /// <param name="meshRenderer">The MeshRenderer to animate.</param>
        /// <param name="alpha">The target alpha value, between 0 and 1.</param>
        /// <param name="duration">The duration of the animation in seconds.</param>
        /// <returns>A configured <see cref="AnimLink.DoAlpha"/> instance ready for playback.</returns>
        static public DoAlpha DoAlpha(this MeshRenderer meshRenderer, float alpha, float duration)
        {
            return new DoAlpha().ConfigDoAlpha(meshRenderer, alpha, duration, AlphaCompType.Mesh);
        }

        #endregion
        #region DoColor

        /// <summary>
        /// Creates a <see cref="AnimLink.DoColor"/> animation for a <see cref="SpriteRenderer"/>.
        /// </summary>
        /// <param name="spriteRenderer">The SpriteRenderer to animate.</param>
        /// <param name="color">The target color.</param>
        /// <param name="duration">The duration of the animation in seconds.</param>
        /// <returns>A configured <see cref="AnimLink.DoColor"/> instance ready for playback.</returns>
        static public DoColor DoColor(this SpriteRenderer spriteRenderer, Color color, float duration)
        {
            return new DoColor().ConfigDoColor(spriteRenderer, color, duration, ColorCompType.Sprite);
        }

        /// <summary>
        /// Creates a <see cref="AnimLink.DoColor"/> animation for a <see cref="Graphic"/> (UI element).
        /// </summary>
        /// <param name="graphic">The Graphic to animate.</param>
        /// <param name="color">The target color.</param>
        /// <param name="duration">The duration of the animation in seconds.</param>
        /// <returns>A configured <see cref="AnimLink.DoColor"/> instance ready for playback.</returns>
        static public DoColor DoColor(this Graphic graphic, Color color, float duration)
        {
            return new DoColor().ConfigDoColor(graphic, color, duration, ColorCompType.Graphic);
        }

        /// <summary>
        /// Creates a <see cref="AnimLink.DoColor"/> animation for a <see cref="Material"/>.
        /// </summary>
        /// <param name="material">The Material to animate.</param>
        /// <param name="color">The target color.</param>
        /// <param name="duration">The duration of the animation in seconds.</param>
        /// <returns>A configured <see cref="AnimLink.DoColor"/> instance ready for playback.</returns>
        static public DoColor DoColor(this Material material, Color color, float duration)
        {
            return new DoColor().ConfigDoColor(material, color, duration, ColorCompType.Material);
        }

        /// <summary>
        /// Creates a <see cref="AnimLink.DoColor"/> animation for a <see cref="MeshRenderer"/>.
        /// <para>Note: This uses MaterialPropertyBlock. To reset properties, use <see cref="MaterialPropertyBlock.Clear(Name_)"/>.</para>
        /// </summary>
        /// <param name="meshRenderer">The MeshRenderer to animate.</param>
        /// <param name="color">The target color.</param>
        /// <param name="duration">The duration of the animation in seconds.</param>
        /// <returns>A configured <see cref="AnimLink.DoColor"/> instance ready for playback.</returns>
        static public DoColor DoColor(this MeshRenderer meshRenderer, Color color, float duration)
        {
            return new DoColor().ConfigDoColor(meshRenderer, color, duration, ColorCompType.Mesh);
        }

        #endregion
        #region DoJolt

        /// <summary>
        /// Creates a <see cref="AnimLink.DoJolt"/> animation for a <see cref="Transform"/>, moving it in a jolt motion.
        /// </summary>
        /// <param name="transform">The Transform to animate.</param>
        /// <param name="direction">The direction and magnitude of the jolt.</param>
        /// <param name="duration">The duration of the animation in seconds.</param>
        /// <param name="vibrato">Number of oscillations (default: 12).</param>
        /// <param name="elasticity">Amount (0–1) the vector overshoots when bouncing back. 1 = full oscillation; 0 = only to start. Default is 0.5.</param>
        /// <returns>A configured <see cref="AnimLink.DoJolt"/> instance ready for playback.</returns>
        public static DoJolt DoJolt(this Transform transform, Vector3 direction, float duration, int vibrato = 12, float elasticity = .5f)
        {
            return new DoJolt().ConfigDoJolt(transform, direction, duration, vibrato, elasticity);
        }

        #endregion
        #region Shake

        /// <summary>
        /// Creates a <see cref="AnimLink.Shake"/> animation for a <see cref="Transform"/>.
        /// </summary>
        /// <param name="transform">The Transform to animate.</param>
        /// <param name="magnitude">The shake magnitude.</param>
        /// <param name="duration">The duration of the animation in seconds.</param>
        /// <param name="activeAxes">Axes to shake along. Can combine multiple values (e.g., Axis.X | Axis.Y).</param>
        /// <returns>A configured <see cref="AnimLink.Shake"/> instance ready for playback.</returns>
        static public Shake Shake(this Transform transform, float magnitude, float duration, Axis activeAxes)
        {
            return new Shake().ConfigShake(transform, magnitude, duration, activeAxes);
        }

        #endregion
        #region AdvancedShake

        /// <summary>
        /// Creates an <see cref="AnimLink.AdvancedShake"/> animation for a <see cref="Transform"/>.
        /// <para>AdvancedShake allows finer control over shake curves and return behavior.</para>
        /// </summary>
        /// <param name="transform">The Transform to animate.</param>
        /// <param name="magnitude">The shake magnitude.</param>
        /// <param name="duration">The duration of the animation in seconds.</param>
        /// <param name="activeAxes">Axes to shake along. Can combine multiple values (e.g., Axis.X | Axis.Y).</param>
        /// <returns>A configured <see cref="AnimLink.AdvancedShake"/> instance ready for playback.</returns>
        static public AdvancedShake AdvancedShake(this Transform transform, float magnitude, float duration, Axis activeAxes)
        {
            return new AdvancedShake().ConfigAdvShake(transform, magnitude, duration, activeAxes);
        }

        #endregion
        #region Every ease type

        static readonly Dictionary<Ease, Func<float, float>> easeFunctions = new()
        {
            { Ease.Linear, t => t },
            { Ease.InSine, EaseInSine },
            { Ease.OutSine, EaseOutSine },
            { Ease.InOutSine, EaseInOutSine },
            { Ease.InQuad, EaseInQuad },
            { Ease.OutQuad, EaseOutQuad },
            { Ease.InOutQuad, EaseInOutQuad },
            { Ease.InCubic, EaseInCubic },
            { Ease.OutCubic, EaseOutCubic },
            { Ease.InOutCubic, EaseInOutCubic },
            { Ease.InQuart, EaseInQuart },
            { Ease.OutQuart, EaseOutQuart },
            { Ease.InOutQuart, EaseInOutQuart },
            { Ease.InQuint, EaseInQuint },
            { Ease.OutQuint, EaseOutQuint },
            { Ease.InOutQuint, EaseInOutQuint },
            { Ease.InExpo, EaseInExpo },
            { Ease.OutExpo, EaseOutExpo },
            { Ease.InOutExpo, EaseInOutExpo },
            { Ease.InCirc, EaseInCirc },
            { Ease.OutCirc, EaseOutCirc },
            { Ease.InOutCirc, EaseInOutCirc },
            { Ease.InBack, EaseInBack },
            { Ease.OutBack, EaseOutBack },
            { Ease.InOutBack, EaseInOutBack },
            { Ease.InElastic, EaseInElastic },
            { Ease.OutElastic, EaseOutElastic },
            { Ease.InOutElastic, EaseInOutElastic },
            { Ease.InBounce, EaseInBounce },
            { Ease.OutBounce, EaseOutBounce },
            { Ease.InOutBounce, EaseInOutBounce }
        };

        /// <summary>
        /// Applies the specified easing function to a normalized time value.
        /// </summary>
        /// <param name="t">Normalized time, typically in the range [0, 1].</param>
        /// <param name="ease">The easing type to apply.</param>
        /// <returns>The eased value corresponding to the input time.</returns>
        static public float EasedT(float t, Ease ease)
        {
            if (easeFunctions.TryGetValue(ease, out var func))
                return func(t);
            // 默认 Linear
            return t;
        }

        static float EaseInSine(float t)
        {
            return 1 - Mathf.Cos(t * Mathf.PI / 2);
        }

        static float EaseOutSine(float t)
        {
            return Mathf.Sin(t * Mathf.PI / 2);
        }

        static float EaseInOutSine(float t)
        {
            return -(Mathf.Cos(t * Mathf.PI) - 1) / 2;
        }

        static float EaseInQuad(float t)
        {
            return t * t;
        }

        static float EaseOutQuad(float t)
        {
            return 1 - (1 - t) * (1 - t);
        }

        static float EaseInOutQuad(float t)
        {
            return t < 0.5 ? 2 * t * t : 1 - Mathf.Pow(-2 * t + 2, 2) / 2;
        }

        static float EaseInCubic(float t)
        {
            return t * t * t;
        }

        static float EaseOutCubic(float t)
        {
            return 1 - Mathf.Pow(1 - t, 3);
        }

        static float EaseInOutCubic(float t)
        {
            return t < 0.5 ? 4 * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 3) / 2;
        }

        static float EaseInQuart(float t)
        {
            return t * t * t * t;
        }

        static float EaseOutQuart(float t)
        {
            return 1 - Mathf.Pow(1 - t, 4);
        }

        static float EaseInOutQuart(float t)
        {
            return t < 0.5 ? 8 * t * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 4) / 2;
        }

        static float EaseInQuint(float t)
        {
            return t * t * t * t * t;
        }

        static float EaseOutQuint(float t)
        {
            return 1 - Mathf.Pow(1 - t, 5);
        }

        static float EaseInOutQuint(float t)
        {
            return t < 0.5 ? 16 * t * t * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 5) / 2;
        }

        static float EaseInExpo(float t)
        {
            return t == 0 ? 0 : Mathf.Pow(2, 10 * t - 10);
        }

        static float EaseOutExpo(float t)
        {
            return t == 1 ? 1 : 1 - Mathf.Pow(2, -10 * t);
        }

        static float EaseInOutExpo(float t)
        {
            return t == 0
            ? 0 : t == 1 ? 1
            : t < 0.5f
            ? Mathf.Pow(2, 20 * t - 10) / 2
            : (2 - Mathf.Pow(2, -20 * t + 10)) / 2;
        }

        static float EaseInCirc(float t)
        {
            return 1 - Mathf.Sqrt(1 - Mathf.Pow(t, 2));
        }

        static float EaseOutCirc(float t)
        {
            return Mathf.Sqrt(1 - Mathf.Pow(t - 1, 2));
        }

        static float EaseInOutCirc(float t)
        {
            return t < 0.5f
            ? (1 - Mathf.Sqrt(1 - Mathf.Pow(2 * t, 2))) / 2
            : (Mathf.Sqrt(1 - Mathf.Pow(-2 * t + 2, 2)) + 1) / 2;
        }

        static float EaseInBack(float t)
        {
            float c1 = 1.70158f;
            float c3 = c1 + 1;
            return c3 * t * t * t - c1 * t * t;
        }

        static float EaseOutBack(float t)
        {
            float c1 = 1.70158f;
            float c3 = c1 + 1;
            return 1 + c3 * Mathf.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2);
        }

        static float EaseInOutBack(float t)
        {
            float c1 = 1.70158f;
            float c2 = c1 * 1.525f;
            return t < 0.5f
            ? Mathf.Pow(2 * t, 2) * ((c2 + 1) * 2 * t - c2) / 2
            : (Mathf.Pow(2 * t - 2, 2) * ((c2 + 1) * (t * 2 - 2) + c2) + 2) / 2;
        }

        static float EaseInElastic(float t)
        {
            float c4 = 2 * Mathf.PI / 3;
            return t == 0 ? 0 : t == 1 ? 1 : -Mathf.Pow(2, 10 * t - 10) * Mathf.Sin((t * 10 - 10.75f) * c4);
        }

        static float EaseOutElastic(float t)
        {
            float c4 = 2 * Mathf.PI / 3;
            return t == 0 ? 0 : t == 1 ? 1 : Mathf.Pow(2, -10 * t) * Mathf.Sin((t * 10 - 0.75f) * c4) + 1;
        }

        static float EaseInOutElastic(float t)
        {
            float c5 = 2 * Mathf.PI / 4.5f;
            return t == 0 ? 0 : t == 1 ? 1 : t < 0.5
            ? -(Mathf.Pow(2, 20 * t - 10) * Mathf.Sin((20 * t - 11.125f) * c5)) / 2
            : Mathf.Pow(2, -20 * t + 10) * Mathf.Sin((20 * t - 11.125f) * c5) / 2 + 1;
        }

        static float EaseInBounce(float t)
        {
            return 1 - EaseOutBounce(1 - t);
        }

        static float EaseOutBounce(float t)
        {
            float d1 = 2.75f;
            float n1 = 7.5625f;
            if (t < 1 / d1)
            {
                return n1 * t * t;
            }
            else if (t < 2 / d1)
            {
                return n1 * (t -= 1.5f / d1) * t + 0.75f;
            }
            else if (t < 2.5f / d1)
            {
                return n1 * (t -= 2.25f / d1) * t + 0.9375f;
            }
            else
            {
                return n1 * (t -= 2.625f / d1) * t + 0.984375f;
            }
        }

        static float EaseInOutBounce(float t)
        {
            return t < 0.5
            ? (1 - EaseOutBounce(1 - 2 * t)) / 2
            : (1 + EaseOutBounce(2 * t - 1)) / 2;
        }

        #endregion
        #region CatmullRom

        /// <summary>
        /// Computes a point on a Catmull-Rom spline given four control points and a normalized parameter.
        /// </summary>
        static public Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            Vector3 a = 2f * p1;
            Vector3 b = p2 - p0;
            Vector3 c = 2f * p0 - 5f * p1 + 4f * p2 - p3;
            Vector3 d = -p0 + 3f * p1 - 3f * p2 + p3;

            Vector3 pos = 0.5f * (a + (b * t) + (t * t * c) + (t * t * t * d));

            return pos;
        }

        #endregion
        #region Cubic Bezier

        /// <summary>
        /// Computes a point on a cubic Bezier curve given four control points and a normalized parameter.
        /// </summary>
        static public Vector3 CubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float u = 1 - t;
            float u2 = u * u;
            float u3 = u2 * u;
            float t2 = t * t;
            float t3 = t2 * t;

            return u3 * p0 + 3 * u2 * t * p1 + 3 * u * t2 * p2 + t3 * p3;
        }

        #endregion
    }

    #region Enums
    public enum Ease
    {
        Linear = 0,
        InSine = 1 << 0,        // 1
        OutSine = 1 << 1,       // 2
        InOutSine = 1 << 2,     // 4
        InQuad = 1 << 3,        // 8
        OutQuad = 1 << 4,       // 16
        InOutQuad = 1 << 5,     // 32
        InCubic = 1 << 6,       // 64
        OutCubic = 1 << 7,      // 128
        InOutCubic = 1 << 8,    // 256
        InQuart = 1 << 9,       // 512
        OutQuart = 1 << 10,     // 1024
        InOutQuart = 1 << 11,   // 2048
        InQuint = 1 << 12,      // 4096
        OutQuint = 1 << 13,     // 8192
        InOutQuint = 1 << 14,   // 16384
        InExpo = 1 << 15,       // 32768
        OutExpo = 1 << 16,      // 65536
        InOutExpo = 1 << 17,    // 131072
        InCirc = 1 << 18,       // 262144
        OutCirc = 1 << 19,      // 524288
        InOutCirc = 1 << 20,    // 1048576
        InBack = 1 << 21,       // 2097152
        OutBack = 1 << 22,      // 4194304
        InOutBack = 1 << 23,    // 8388608
        InElastic = 1 << 24,    // 16777216
        OutElastic = 1 << 25,   // 33554432
        InOutElastic = 1 << 26, // 67108864
        InBounce = 1 << 27,     // 134217728
        OutBounce = 1 << 28,    // 268435456
        InOutBounce = 1 << 29   // 536870912
    }

    /// <summary>
    /// Defines the loop type for an animation.
    /// </summary>
    public enum BaseLoop
    {
        /// <summary>
        /// Each loop starts from the initial value.
        /// </summary>
        FromStart = 0,
        /// <summary>
        /// Ping-pong loop. The animation plays forward, then backward, and repeats.
        /// </summary>
        PingPong = 1,
        /// <summary>
        /// Incremental loop. Each loop continues from the last target value, accumulating the effect.
        /// </summary>
        Increment = 2,
    }

    /// <summary>
    /// Defines the loop type for an animation.
    /// </summary>
    public enum ValueLoop
    {
        /// <summary>
        /// Each loop starts from the initial value.
        /// </summary>
        FromStart = 0,
        /// <summary>
        /// Ping-pong loop. The animation plays forward, then backward, and repeats.
        /// </summary>
        PingPong = 1,
        /// <summary>
        /// Incremental loop. Each loop continues from the last target value, accumulating the effect.
        /// </summary>
        Increment = 2,
        /// <summary>
        /// Decremental loop. Each loop continues from the last target value, subtracting the change.
        /// </summary>
        Decrement = 4
    }

    /// <summary>
    /// Defines the loop type for an animation.
    /// </summary>
    public enum PathLoop
    {
        /// <summary>
        /// Each loop starts from the initial value.
        /// </summary>
        FromStart = 0,
        /// <summary>
        /// Ping-pong loop. The animation plays forward, then backward, and repeats.
        /// </summary>
        PingPong = 1,
        /// <summary>
        /// Incremental loop. Each loop continues from the last target value, accumulating the effect.
        /// </summary>
        Increment = 2,
        /// <summary>
        /// Continuous loop. The path forms a closed loop, where the endpoint connects back to the start.
        /// </summary>
        Loop = 4,
    }

    public enum Axis { None = 0, X = 1, Y = 2, Z = 4 }

    public enum PathType
    {
        LinearPath = 0,
        CatmullRom = 1,
        CubicBezier = 2,
    }

    public enum RotateMode
    {
        /// <summary>
        /// Standard rotation.
        /// </summary>
        Fast,
        /// <summary>
        /// Allows rotation beyond 360°.
        /// </summary>
        FastBeyond360
    }

    public enum ColorCompType
    {
        Sprite = 0,
        Material = 1,
        Graphic = 2,
        Mesh = 4,
    }

    public enum AlphaCompType
    {
        Sprite = 0,
        Material = 1,
        Graphic = 2,
        Mesh = 4,
        CanvasGroup = 8,
    }

    public enum PosScale
    {
        Position = 0,
        Scale = 1,
    }

    public enum PosScaleRot
    {
        Position = 0,
        Scale = 1,
        Rotation = 2,
    }

    public enum Dimension
    {
        _2D,
        _3D
    }
    #endregion
}