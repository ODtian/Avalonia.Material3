# preview.4 overlay motion

| Family | Default reference recipe |
|---|---|
| Menu | pinned `Menu.kt`:scale.8↔1/FastSpatial, alpha0↔1/FastEffects; anchor-intersection pivot |
| Tooltip / Snackbar | pinned `Tooltip.kt` / `SnackbarHost.kt`:scale.8↔1/FastSpatial, alpha0↔1/FastEffects; center pivot |
| Modal / dismissible standard drawer | pinned `NavigationDrawer.kt`:DefaultSpatial open, FastEffects close; scrim/content follow drawer offset; surface overshoot and inverse child scaling |
| MaterialDialog | Compose AndroidDialog delegates to window-theme animation. Concrete AOSP `android-16.0.0_r1` Material.Dialog:220ms alpha0→1/Y20→0, cubic(.2,0,0,1);150ms alpha1→0/Y0→−10, cubic(.3,0,1,1) |

The three Compose sources retain AndroidX pin `11ece46a49d485c7644e53cb0684a611d7a0ec10`. The additional dialog platform reference is [AOSP Material animation styles](https://github.com/aosp-mirror/platform_frameworks_base/blob/android-16.0.0_r1/core/res/res/values/styles_material.xml), [enter](https://github.com/aosp-mirror/platform_frameworks_base/blob/android-16.0.0_r1/core/res/res/anim/popup_enter_material.xml), [exit](https://github.com/aosp-mirror/platform_frameworks_base/blob/android-16.0.0_r1/core/res/res/anim/popup_exit_material.xml), with dimensions/config/interpolator values from the same tag.

Closing results, focus, modality and original-control unparenting remain synchronous. The exit carrier is a disabled, noninteractive attached-layer raster including64-DIP surface overflow. Live/manual motion uses the shared frame channel. Generic host content preserves its presentation; reduced motion snaps standard component endpoints.

Eight new public layout/property/raster/lifecycle scenarios recorded red→green. Corresponding family regression:61 passed initially; four former static position assertions explicitly select reduced motion; the11 affected/new/teardown cases then passed. Supplementary resize and swipe-close cases verify frozen exit location and continuity from the dragged offset; the final10 motion/gesture cases pass. Tests observe public transforms, opacity, content reservation, pixel changes, original-parent release, results, reopening and reduced motion. Public spring overrides extend tracks for headless sampling. Exact source hashes and integration handoff are in external `F:/PixivYou/analysis/m3-standard-round2/overlay.md`.
