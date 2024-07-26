#nullable enable
using System.Security.Cryptography;

namespace Terminal.Gui;

/// <summary>
///     Helper class for <see cref="Application"/> navigation.
/// </summary>
internal static class ApplicationNavigation
{
    /// <summary>
    ///    Gets the deepest focused subview of the specified <paramref name="view"/>.
    /// </summary>
    /// <param name="view"></param>
    /// <returns></returns>
    internal static View? GetDeepestFocusedSubview (View? view)
    {
        if (view is null)
        {
            return null;
        }

        foreach (View v in view.Subviews)
        {
            if (v.HasFocus)
            {
                return GetDeepestFocusedSubview (v);
            }
        }

        return view;
    }

    /// <summary>
    /// Sets the focus to the next view in the specified direction within the provided list of views.
    /// If the end of the list is reached, the focus wraps around to the first view in the list.
    /// The method considers the current focused view (`Application.Current`) and attempts to move the focus
    /// to the next view in the specified direction. If the focus cannot be set to the next view, it wraps around
    /// to the first view in the list.
    /// </summary>
    /// <param name="viewsInTabIndexes"></param>
    /// <param name="direction"></param>
    internal static void SetFocusToNextViewWithWrap (IEnumerable<View>? viewsInTabIndexes, NavigationDirection direction)
    {
        if (viewsInTabIndexes is null)
        {
            return;
        }

        bool foundCurrentView = false;
        bool focusSet = false;
        IEnumerable<View> indexes = viewsInTabIndexes as View [] ?? viewsInTabIndexes.ToArray ();
        int viewCount = indexes.Count ();
        int currentIndex = 0;

        foreach (View view in indexes)
        {
            if (view == Application.Current)
            {
                foundCurrentView = true;
            }
            else if (foundCurrentView && !focusSet)
            {
                // One of the views is Current, but view is not. Attempt to Advance...
                Application.Current!.SuperView?.AdvanceFocus (direction);
                // QUESTION: AdvanceFocus returns false AND sets Focused to null if no view was found to advance to. Should't we only set focusProcessed if it returned true?
                focusSet = true;

                if (Application.Current.SuperView?.Focused != Application.Current)
                {
                    return;
                }

                // Either AdvanceFocus didn't set focus or the view it set focus to is not current...
                // continue...
            }

            currentIndex++;

            if (foundCurrentView && !focusSet && currentIndex == viewCount)
            {
                // One of the views is Current AND AdvanceFocus didn't set focus AND we are at the last view in the list...
                // This means we should wrap around to the first view in the list.
                indexes.First ().SetFocus ();
            }
        }
    }

    /// <summary>
    ///     Moves the focus to the next focusable view.
    ///     Honors <see cref="ViewArrangement.Overlapped"/> and will only move to the next subview
    ///     if the current and next subviews are not overlapped.
    /// </summary>
    internal static void MoveNextView ()
    {
        View? old = GetDeepestFocusedSubview (Application.Current!.Focused);

        if (!Application.Current.AdvanceFocus (NavigationDirection.Forward))
        {
            Application.Current.AdvanceFocus (NavigationDirection.Forward);
        }

        if (old != Application.Current.Focused && old != Application.Current.Focused?.Focused)
        {
            old?.SetNeedsDisplay ();
            Application.Current.Focused?.SetNeedsDisplay ();
        }
        else
        {
            SetFocusToNextViewWithWrap (Application.Current.SuperView?.TabIndexes, NavigationDirection.Forward);
        }
    }

    /// <summary>
    ///     Moves the focus to the next <see cref="Toplevel"/> subview or the next subview that has <see cref="ApplicationOverlapped.OverlappedTop"/> set.
    /// </summary>
    internal static void MoveNextViewOrTop ()
    {
        if (ApplicationOverlapped.OverlappedTop is null)
        {
            Toplevel? top = Application.Current!.Modal ? Application.Current : Application.Top;

            if (!Application.Current.AdvanceFocus (NavigationDirection.Forward))
            {
                Application.Current.AdvanceFocus (NavigationDirection.Forward);
            }

            if (top != Application.Current.Focused && top != Application.Current.Focused?.Focused)
            {
                top?.SetNeedsDisplay ();
                Application.Current.Focused?.SetNeedsDisplay ();
            }
            else
            {
                SetFocusToNextViewWithWrap (Application.Current.SuperView?.TabIndexes, NavigationDirection.Forward);
            }



            //top!.AdvanceFocus (NavigationDirection.Forward);

            //if (top.Focused is null)
            //{
            //    top.AdvanceFocus (NavigationDirection.Forward);
            //}

            //top.SetNeedsDisplay ();
            ApplicationOverlapped.BringOverlappedTopToFront ();
        }
        else
        {
            ApplicationOverlapped.OverlappedMoveNext ();
        }
    }

    /// <summary>
    ///     Moves the focus to the next view. Honors <see cref="ViewArrangement.Overlapped"/> and will only move to the next subview
    ///     if the current and next subviews are not overlapped.
    /// </summary>
    internal static void MovePreviousView ()
    {
        View? old = GetDeepestFocusedSubview (Application.Current!.Focused);

        if (!Application.Current.AdvanceFocus (NavigationDirection.Backward))
        {
            Application.Current.AdvanceFocus (NavigationDirection.Backward);
        }

        if (old != Application.Current.Focused && old != Application.Current.Focused?.Focused)
        {
            old?.SetNeedsDisplay ();
            Application.Current.Focused?.SetNeedsDisplay ();
        }
        else
        {
            SetFocusToNextViewWithWrap (Application.Current.SuperView?.TabIndexes?.Reverse (), NavigationDirection.Backward);
        }
    }

    internal static void MovePreviousViewOrTop ()
    {
        if (ApplicationOverlapped.OverlappedTop is null)
        {
            Toplevel? top = Application.Current!.Modal ? Application.Current : Application.Top;
            top!.AdvanceFocus (NavigationDirection.Backward);

            if (top.Focused is null)
            {
                top.AdvanceFocus (NavigationDirection.Backward);
            }

            top.SetNeedsDisplay ();
            ApplicationOverlapped.BringOverlappedTopToFront ();
        }
        else
        {
            ApplicationOverlapped.OverlappedMovePrevious ();
        }
    }
}
