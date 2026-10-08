@file:OptIn(androidx.compose.material3.ExperimentalMaterial3Api::class, androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class, androidx.compose.ui.ExperimentalComposeUiApi::class)

package org.pixivyou.m3reference

import android.os.Bundle
import android.content.res.Configuration
import android.os.LocaleList
import android.os.Build
import android.os.SystemClock
import android.view.MotionEvent
import android.util.Log
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.LocalConfiguration
import androidx.compose.ui.platform.LocalResources
import androidx.compose.ui.platform.LocalLocale
import androidx.compose.ui.platform.LocalProvidableLocaleList
import androidx.compose.ui.platform.LocalView
import androidx.compose.ui.input.pointer.motionEventSpy
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.semantics.testTagsAsResourceId
import androidx.compose.ui.state.ToggleableState
import androidx.compose.ui.unit.dp
import java.time.LocalDate
import java.time.ZoneOffset
import java.util.Locale

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        val initialScene = intent.getStringExtra("scene") ?: "home"
        val initialDark = intent.getBooleanExtra("dark", false)
        val palette = intent.getStringExtra("palette") ?: "classic"
        val localeTag = intent.getStringExtra("locale") ?: "en-US"
        Locale.setDefault(Locale.forLanguageTag(localeTag))
        val expressiveButtons = intent.getBooleanExtra("expressiveButtons", true)
        // The release defaults are preserved; this explicit flag selects the M3 checkbox
        // migration branch for comparison with a library implementing the new M3 styling.
        ComposeMaterial3Flags.isCheckboxStylingFixEnabled = intent.getBooleanExtra("checkboxM3", true)
        setContent {
            val baseContext = LocalContext.current
            val baseConfiguration = LocalConfiguration.current
            val configuration = remember(baseConfiguration, localeTag) {
                Configuration(baseConfiguration).apply { setLocales(LocaleList.forLanguageTags(localeTag)) }
            }
            val localizedContext = remember(baseContext, configuration) {
                android.view.ContextThemeWrapper(baseContext, R.style.ReferenceTheme).apply {
                    applyOverrideConfiguration(configuration)
                }
            }
            CompositionLocalProvider(LocalContext provides localizedContext, LocalConfiguration provides configuration,
                LocalResources provides localizedContext.resources, LocalProvidableLocaleList provides androidx.compose.ui.text.intl.LocaleList(configuration.locales.toLanguageTags()),
                LocalNativeExpressiveButtons provides expressiveButtons) {
                ReferenceApp(initialScene, initialDark, palette)
            }
        }
        window.decorView.post { Log.i("M3Reference", "activityDecorHardwareAccelerated=${window.decorView.isHardwareAccelerated}") }
    }
}

internal val LocalNativeExpressiveButtons = staticCompositionLocalOf { true }
private val scenes = listOf("date-range", "date-range-7-24", "date-range-9-16", "date-single", "time", "selection", "slider", "fields", "buttons", "ripple", "fab", "progress", "carousel", "navigation", "overlays")

@Composable
private fun ReferenceApp(initialScene: String, initialDark: Boolean, palette: String) {
    var scene by remember { mutableStateOf(initialScene) }
    var dark by remember { mutableStateOf(initialDark) }
    MaterialExpressiveTheme(colorScheme = if (dark) darkColorScheme() else if (palette == "classic") lightColorScheme() else expressiveLightColorScheme()) {
        val colors = MaterialTheme.colorScheme
        val density = LocalDensity.current
        val view = LocalView.current
        val locale = LocalLocale.current
        val expressiveButtons = LocalNativeExpressiveButtons.current
        LaunchedEffect(dark, palette) {
            Log.i("M3Reference", "material3=1.5.0-beta01 sdk=${Build.VERSION.SDK_INT} scene=$initialScene dark=$dark palette=$palette locale=${locale.toLanguageTag()} density=${density.density} fontScale=${density.fontScale} expressiveButtons=$expressiveButtons checkboxM3=${ComposeMaterial3Flags.isCheckboxStylingFixEnabled} timeToggle=${ComposeMaterial3Flags.isUpdatedTimepickerToggleEnabled} colors=$colors")
            view.post { Log.i("M3Reference", "composeRootHardwareAccelerated=${view.isHardwareAccelerated} rootClass=${view.javaClass.name}") }
        }
        Surface(Modifier.fillMaxSize().motionEventSpy { event ->
            if (event.actionMasked == MotionEvent.ACTION_DOWN || event.actionMasked == MotionEvent.ACTION_UP || event.actionMasked == MotionEvent.ACTION_CANCEL) {
                Log.i("M3Reference", "pointer action=${event.actionMasked} eventUptime=${event.eventTime} observedUptime=${SystemClock.uptimeMillis()} x=${event.x} y=${event.y} rawX=${event.rawX} rawY=${event.rawY}")
            }
        }.semantics { testTagsAsResourceId = true }) {
            Scaffold(topBar = {
                TopAppBar(title = { Text("M3 · $scene") }, navigationIcon = {
                    if (scene != "home") IconButton(onClick = { scene = "home" }, modifier = Modifier.testTag("home")) { Icon(Icons.AutoMirrored.Filled.ArrowBack, "Home") }
                }, actions = {
                    IconButton(onClick = { dark = !dark }, modifier = Modifier.testTag("theme")) { Icon(if (dark) Icons.Default.LightMode else Icons.Default.DarkMode, "Toggle theme") }
                })
            }) { padding ->
                Column(Modifier.fillMaxSize().padding(padding)) {
                    when (scene) {
                        "home" -> Column(Modifier.verticalScroll(rememberScrollState()).padding(16.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
                            Text("Official Compose Material3 1.5.0-beta01", style = MaterialTheme.typography.bodySmall)
                            scenes.forEach { id -> FilledTonalButton(onClick = { scene = id }, modifier = Modifier.fillMaxWidth().testTag("scene-$id")) { Text(id) } }
                        }
                        "date-range", "date-range-7-24", "date-range-9-16" -> DateRangeScene(scene)
                        "date-single" -> DateSingleScene()
                        "time" -> TimeScene()
                        "selection" -> SelectionScene()
                        "slider" -> SliderScene()
                        "fields" -> FieldsScene()
                        else -> ExtendedScene(scene)
                    }
                }
            }
        }
    }
}

private fun feb(day: Int): Long = LocalDate.of(2024, 2, day).atStartOfDay(ZoneOffset.UTC).toInstant().toEpochMilli()

@Composable
private fun DateRangeScene(scene: String) {
    val dialogConfiguration = LocalConfiguration.current
    val dialogLocales = LocalProvidableLocaleList.current
    val start = when (scene) { "date-range-7-24" -> 7; "date-range-9-16" -> 9; else -> 10 }
    val end = when (scene) { "date-range-7-24" -> 24; "date-range-9-16" -> 16; else -> 12 }
    val state = rememberDateRangePickerState(initialSelectedStartDateMillis = feb(start), initialSelectedEndDateMillis = feb(end), initialDisplayedMonthMillis = feb(1), yearRange = 2024..2024,
        selectableDates = object : SelectableDates { override fun isSelectableDate(utcTimeMillis: Long) = utcTimeMillis != feb(20) })
    var modal by remember { mutableStateOf(false) }
    Row(Modifier.padding(horizontal = 12.dp), horizontalArrangement = Arrangement.spacedBy(8.dp)) {
        TextButton(onClick = { state.setSelection(feb(10), feb(12)) }, modifier = Modifier.testTag("reset-range")) { Text("Reset 10–12") }
        TextButton(onClick = { modal = true }, modifier = Modifier.testTag("open-date-dialog")) { Text("Dialog") }
    }
    Text("${state.selectedStartDateMillis?.let { java.time.Instant.ofEpochMilli(it).atZone(ZoneOffset.UTC).dayOfMonth }} – ${state.selectedEndDateMillis?.let { java.time.Instant.ofEpochMilli(it).atZone(ZoneOffset.UTC).dayOfMonth }}", Modifier.padding(horizontal = 16.dp).testTag("selected-range"))
    DateRangePicker(state = state, modifier = Modifier.weightForScene().testTag("date-range-picker"))
    if (modal) DatePickerDialog(onDismissRequest = { modal = false }, confirmButton = { TextButton(onClick = { modal = false }) { Text("OK") } }, dismissButton = { TextButton(onClick = { modal = false }) { Text("Cancel") } }) {
        CompositionLocalProvider(LocalConfiguration provides dialogConfiguration, LocalProvidableLocaleList provides dialogLocales) {
            DateRangePicker(state = state, modifier = Modifier.height(540.dp).testTag("date-range-dialog-picker"))
        }
    }
}

private fun Modifier.weightForScene(): Modifier = fillMaxWidth().fillMaxHeight()

@Composable
private fun DateSingleScene() {
    val state = rememberDatePickerState(initialSelectedDateMillis = feb(9), initialDisplayedMonthMillis = feb(1), yearRange = 2024..2024)
    DatePicker(state = state, modifier = Modifier.testTag("date-picker"))
}

@Composable
private fun TimeScene() {
    val state = rememberTimePickerState(initialHour = 19, initialMinute = 7, is24Hour = false)
    var modal by remember { mutableStateOf(false) }
    var input by remember { mutableStateOf(false) }
    Row(Modifier.padding(12.dp), horizontalArrangement = Arrangement.spacedBy(8.dp)) {
        TextButton(onClick = { modal = true }, modifier = Modifier.testTag("open-time-dialog")) { Text("Dialog") }
        TextButton(onClick = { input = !input }, modifier = Modifier.testTag("time-input-toggle")) { Text("Keyboard") }
    }
    Box(Modifier.fillMaxWidth().padding(16.dp), contentAlignment = androidx.compose.ui.Alignment.TopCenter) {
        if (input) TimeInput(state = state, modifier = Modifier.testTag("time-input")) else TimePicker(state = state, modifier = Modifier.testTag("time-picker"))
    }
    if (modal) {
        val mode = if (input) TimePickerDisplayMode.Input else TimePickerDisplayMode.Picker
        TimePickerDialog(
            onDismissRequest = { modal = false },
            title = { TimePickerDialogDefaults.Title(displayMode = mode) },
            modeToggleButton = {
                TimePickerDialogDefaults.DisplayModeToggle(
                    onDisplayModeChange = { input = !input }, displayMode = mode,
                    modifier = Modifier.testTag("time-dialog-mode"))
            },
            confirmButton = { TextButton(onClick = { modal = false }) { Text("OK") } },
            dismissButton = { TextButton(onClick = { modal = false }) { Text("Cancel") } },
        ) { if (input) TimeInput(state = state) else TimePicker(state = state) }
    }
}

@Composable
private fun SelectionScene() = SceneColumn {
    var check by remember { mutableStateOf(false) }
    var mixed by remember { mutableStateOf(ToggleableState.Indeterminate) }
    var radio by remember { mutableIntStateOf(0) }
    var switch by remember { mutableStateOf(true) }
    Text("Checkbox / Radio / Switch", style = MaterialTheme.typography.titleLarge)
    Row(verticalAlignment = androidx.compose.ui.Alignment.CenterVertically) { Checkbox(check, { check = it }, Modifier.testTag("checkbox")); Text("Notifications") }
    Row(verticalAlignment = androidx.compose.ui.Alignment.CenterVertically) { TriStateCheckbox(mixed, { mixed = if (mixed == ToggleableState.On) ToggleableState.Off else ToggleableState.On }, Modifier.testTag("checkbox-mixed")); Text("Mixed state") }
    Row(verticalAlignment = androidx.compose.ui.Alignment.CenterVertically) { Checkbox(true, {}, enabled = false); Text("Disabled") }
    repeat(3) { index -> Row(verticalAlignment = androidx.compose.ui.Alignment.CenterVertically) { RadioButton(radio == index, { radio = index }, Modifier.testTag("radio-$index")); Text("Choice ${index + 1}") } }
    Row(verticalAlignment = androidx.compose.ui.Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(12.dp)) {
        Switch(switch, { switch = it }, Modifier.testTag("switch")); Text("Switch")
    }
    Row(verticalAlignment = androidx.compose.ui.Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(12.dp)) {
        Switch(switch, { switch = it }, Modifier.testTag("switch-icons"), thumbContent = { Icon(if (switch) Icons.Default.Check else Icons.Default.Close, null, Modifier.size(SwitchDefaults.IconSize)) }); Text("Thumb icons")
    }
}

@Composable
private fun SliderScene() = SceneColumn {
    val continuous = remember { SliderState(value = .27f) }
    val discrete = remember { SliderState(value = 70f, steps = 9, trackRange = 0f..100f) }
    val range = remember { RangeSliderState(startValue = 8f, endValue = 20f, steps = 23, trackRange = 0f..24f) }
    Text("Slider · continuous", style = MaterialTheme.typography.titleMedium)
    Slider(state = continuous, onValueChange = { continuous.value = it }, modifier = Modifier.testTag("slider"))
    Text("Volume · discrete with marks", style = MaterialTheme.typography.titleMedium)
    Slider(state = discrete, onValueChange = { discrete.value = it }, modifier = Modifier.testTag("slider-discrete"))
    Text("Active hours · ordered range", style = MaterialTheme.typography.titleMedium)
    RangeSlider(state = range, onValueChange = { range.startValue = it.start; range.endValue = it.endInclusive }, modifier = Modifier.testTag("slider-range"))
}

@Composable
private fun FieldsScene() = SceneColumn {
    var value by remember { mutableStateOf("") }
    OutlinedTextField(value, { value = it }, label = { Text("Display name") }, supportingText = { Text("Enter a non-empty name") }, modifier = Modifier.fillMaxWidth().testTag("field-outlined"))
    OutlinedTextField(value, { value = it }, label = { Text("Amount / 金额") }, leadingIcon = { Icon(Icons.Default.Diamond, "Amount") }, trailingIcon = { Icon(Icons.Default.Check, "Valid") }, prefix = { Text("¥") }, suffix = { Text("CNY") }, modifier = Modifier.fillMaxWidth().testTag("field-amount"))
    TextField(value, { value = it }, label = { Text("Sheet editor") }, supportingText = { Text("Editable draft") }, modifier = Modifier.fillMaxWidth().testTag("field-filled"))
}

@Composable
internal fun SceneColumn(content: @Composable ColumnScope.() -> Unit) {
    Column(Modifier.fillMaxSize().verticalScroll(rememberScrollState()).padding(16.dp), verticalArrangement = Arrangement.spacedBy(16.dp), content = content)
}

