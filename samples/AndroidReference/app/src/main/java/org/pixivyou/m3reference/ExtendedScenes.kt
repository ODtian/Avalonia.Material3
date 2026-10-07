@file:OptIn(androidx.compose.material3.ExperimentalMaterial3Api::class, androidx.compose.material3.ExperimentalMaterial3ExpressiveApi::class)
package org.pixivyou.m3reference
import androidx.compose.foundation.Image
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material.icons.automirrored.filled.Reply
import androidx.compose.material.icons.automirrored.filled.Forward
import androidx.compose.material3.*
import androidx.compose.material3.carousel.*
import androidx.compose.material3.ToggleFloatingActionButtonDefaults.animateIcon
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import android.widget.LinearLayout
import android.widget.TextView
import com.google.android.material.sidesheet.SideSheetDialog

@Composable
internal fun ExtendedScene(scene: String) {
    when (scene) {
        "buttons" -> ButtonsScene()
        "ripple" -> RippleScene()
        "fab" -> FabScene()
        "progress" -> ProgressScene()
        "carousel" -> CarouselScene()
        "navigation" -> NavigationScene()
        "overlays" -> OverlaysScene()
    }
}

@Composable
private fun RippleScene() = SceneColumn {
    var count by remember { mutableIntStateOf(0) }
    Text("Pressed $count", Modifier.testTag("ripple-count"))
    Text("Hold and release the official buttons", style = MaterialTheme.typography.titleMedium)
    Button(onClick = { count++ }, modifier = Modifier.fillMaxWidth().testTag("ripple-filled")) { Text("Filled button") }
    ElevatedButton(onClick = { count++ }, modifier = Modifier.fillMaxWidth().testTag("ripple-elevated")) { Text("Elevated button") }
    FilledTonalButton(onClick = { count++ }, modifier = Modifier.fillMaxWidth().testTag("ripple-tonal")) { Text("Tonal button") }
    OutlinedButton(onClick = { count++ }, modifier = Modifier.fillMaxWidth().testTag("ripple-outlined")) { Text("Outlined button") }
    TextButton(onClick = { count++ }, modifier = Modifier.fillMaxWidth().testTag("ripple-text")) { Text("Text button") }
    Row(horizontalArrangement = Arrangement.spacedBy(24.dp)) {
        IconButton(onClick = { count++ }, modifier = Modifier.testTag("ripple-icon")) { Icon(Icons.Default.Edit, "Icon button") }
        FloatingActionButton(onClick = { count++ }, modifier = Modifier.testTag("ripple-fab")) { Icon(Icons.Default.Add, "FAB") }
    }
}

@Composable
private fun ButtonsScene() = SceneColumn {
    var saves by remember { mutableIntStateOf(0) }
    var selected by remember { mutableIntStateOf(0) }
    Text("Saved: $saves", Modifier.testTag("saved"))
    Text("Unconnected — None", style = MaterialTheme.typography.titleMedium)
    ButtonGroup(overflowIndicator = { ButtonGroupDefaults.OverflowIndicator(it) }, modifier = Modifier.testTag("button-group")) {
        clickableItem(onClick = { saves++ }, label = "Create")
        clickableItem(onClick = { saves++ }, label = "Edit")
        clickableItem(onClick = { saves++ }, label = "Share")
        clickableItem(onClick = {}, label = "Disabled", enabled = false)
    }
    Text("Unconnected — Single", style = MaterialTheme.typography.titleMedium)
    ButtonGroup(overflowIndicator = { ButtonGroupDefaults.OverflowIndicator(it) }, modifier = Modifier.testTag("button-group-single")) {
        listOf("Photos", "Videos", "Audio").forEachIndexed { index, label ->
            toggleableItem(checked = selected == index, label = label, onCheckedChange = { selected = index }, icon = { Icon(if (selected == index) Icons.Default.Check else Icons.Default.FavoriteBorder, null) })
        }
    }
    Text("Connected — Single", style = MaterialTheme.typography.titleMedium)
    Row(horizontalArrangement = Arrangement.spacedBy(ButtonGroupDefaults.ConnectedSpaceBetween), modifier = Modifier.testTag("connected-group")) {
        listOf("Photos", "Videos", "Audio").forEachIndexed { index, label ->
            ToggleButton(checked = selected == index, onCheckedChange = { selected = index }, shapes = when(index) {
                0 -> ButtonGroupDefaults.connectedLeadingButtonShapes()
                2 -> ButtonGroupDefaults.connectedTrailingButtonShapes()
                else -> ButtonGroupDefaults.connectedMiddleButtonShapes()
            }) { Text(label) }
        }
    }
    listOf("Extra small" to SplitButtonDefaults.ExtraSmallContainerHeight, "Small" to SplitButtonDefaults.SmallContainerHeight,
        "Medium" to SplitButtonDefaults.MediumContainerHeight, "Large" to SplitButtonDefaults.LargeContainerHeight,
        "Extra large" to SplitButtonDefaults.ExtraLargeContainerHeight).forEach { (label, height) ->
        Text("Elevated · $label", style = MaterialTheme.typography.titleMedium)
        ReferenceSplitButton(label, height, elevated = true, onSave = { saves++ })
    }
    ReferenceSplitButton("Save to Local", SplitButtonDefaults.SmallContainerHeight, elevated = false, onSave = { saves++ })
    ElevatedButton(onClick = { saves++ }, modifier = Modifier.testTag("elevated-button")) { Icon(Icons.Default.Add, null); Spacer(Modifier.width(ButtonDefaults.IconSpacing)); Text("Elevated") }
    ElevatedCard(Modifier.fillMaxWidth()) { Text("Elevated card", Modifier.padding(24.dp)) }
}

@Composable
private fun ReferenceSplitButton(label: String, height: Dp, elevated: Boolean, onSave: () -> Unit) {
    var expanded by remember { mutableStateOf(false) }
    val colors = if (elevated) ButtonDefaults.elevatedButtonColors() else ButtonDefaults.buttonColors()
    val elevation = if (elevated) ButtonDefaults.elevatedButtonElevation() else ButtonDefaults.buttonElevation()
    SplitButtonLayout(modifier = Modifier.testTag("split-${label.replace(' ', '-')}").fillMaxWidth(), leadingButton = {
        SplitButtonDefaults.LeadingButton(onClick = onSave, modifier = Modifier.heightIn(min = height), shapes = SplitButtonDefaults.leadingButtonShapesFor(height),
            colors = colors, elevation = elevation, contentPadding = SplitButtonDefaults.leadingButtonContentPaddingFor(height)) {
            Icon(Icons.Default.Add, null, Modifier.size(SplitButtonDefaults.leadingButtonIconSizeFor(height)))
            Spacer(Modifier.width(ButtonDefaults.iconSpacingFor(height)))
            Text(label, style = ButtonDefaults.textStyleFor(height))
        }
    }, trailingButton = {
        Box {
            SplitButtonDefaults.TrailingButton(checked = expanded, onCheckedChange = { expanded = it }, modifier = Modifier.heightIn(min = height).testTag("split-toggle-${label.replace(' ', '-')}") ,
                shapes = SplitButtonDefaults.trailingButtonShapesFor(height), colors = colors, elevation = elevation, contentPadding = SplitButtonDefaults.trailingButtonContentPaddingFor(height)) {
                Icon(Icons.Default.KeyboardArrowDown, "Choose destination", Modifier.size(SplitButtonDefaults.trailingButtonIconSizeFor(height)))
            }
            DropdownMenu(expanded, { expanded = false }) {
                listOf("Local", "Cloud", "Shared").forEach { destination -> DropdownMenuItem(text = { Text(destination) }, onClick = { expanded = false; onSave() }) }
            }
        }
    })
}

@Composable
private fun FabScene() {
    var expanded by remember { mutableStateOf(false) }
    var toolbarExpanded by remember { mutableStateOf(true) }
    Box(Modifier.fillMaxSize().padding(16.dp)) {
        Column(verticalArrangement = Arrangement.spacedBy(16.dp)) {
            Row(horizontalArrangement = Arrangement.spacedBy(16.dp)) {
                SmallFloatingActionButton(onClick = {}) { Icon(Icons.Default.Edit, "Small FAB") }
                FloatingActionButton(onClick = {}) { Icon(Icons.Default.Edit, "FAB") }
                LargeFloatingActionButton(onClick = {}) { Icon(Icons.Default.Edit, "Large FAB") }
            }
            ExtendedFloatingActionButton(onClick = {}) { Icon(Icons.Default.Add, null); Spacer(Modifier.width(8.dp)); Text("Create") }
            TextButton(onClick = { toolbarExpanded = !toolbarExpanded }, modifier = Modifier.testTag("toolbar-toggle")) { Text("Toggle toolbar") }
            HorizontalFloatingToolbar(expanded = toolbarExpanded, modifier = Modifier.testTag("floating-toolbar"), leadingContent = { IconButton(onClick = {}) { Icon(Icons.Default.Menu, "Menu") } }, trailingContent = { IconButton(onClick = {}) { Icon(Icons.Default.MoreVert, "More") } }) {
                IconButton(onClick = {}) { Icon(Icons.Default.Edit, "Edit") }
                IconButton(onClick = {}) { Icon(Icons.Default.Share, "Share") }
            }
        }
        FloatingActionButtonMenu(expanded = expanded, modifier = Modifier.align(Alignment.BottomEnd).testTag("fab-menu"), button = {
            ToggleFloatingActionButton(checked = expanded, onCheckedChange = { expanded = it }, modifier = Modifier.testTag("fab-menu-toggle")) {
                Icon(if (checkedProgress > .5f) Icons.Default.Close else Icons.Default.Add, "Create actions", Modifier.animateIcon({ checkedProgress }))
            }
        }) {
            FloatingActionButtonMenuItem(onClick = { expanded = false }, text = { Text("Reply") }, icon = { Icon(Icons.AutoMirrored.Filled.Reply, null) })
            FloatingActionButtonMenuItem(onClick = { expanded = false }, text = { Text("Reply all") }, icon = { Icon(Icons.Default.People, null) })
            FloatingActionButtonMenuItem(onClick = { expanded = false }, text = { Text("Forward") }, icon = { Icon(Icons.AutoMirrored.Filled.Forward, null) })
        }
    }
}

@Composable
private fun ProgressScene() = SceneColumn {
    Text("Determinate", style = MaterialTheme.typography.titleMedium)
    LinearProgressIndicator(progress = { .42f }, modifier = Modifier.fillMaxWidth())
    CircularProgressIndicator(progress = { .42f })
    Text("Indeterminate", style = MaterialTheme.typography.titleMedium)
    LinearProgressIndicator(modifier = Modifier.fillMaxWidth().testTag("linear-progress"))
    CircularProgressIndicator(Modifier.testTag("circular-progress"))
    Text("Expressive", style = MaterialTheme.typography.titleMedium)
    LinearWavyProgressIndicator(modifier = Modifier.fillMaxWidth().testTag("wavy-progress"))
    CircularWavyProgressIndicator(Modifier.testTag("circular-wavy-progress"))
    LoadingIndicator(Modifier.testTag("loading-indicator"))
    ContainedLoadingIndicator(Modifier.testTag("contained-loading-indicator"))
}

@Composable
private fun CarouselScene() = SceneColumn {
    var narrow by remember { mutableStateOf(false) }
    val state = rememberCarouselState { 6 }
    TextButton(onClick = { narrow = !narrow }, modifier = Modifier.testTag("carousel-layout-toggle")) { Text("Narrow / wide") }
    Text("Multi-browse · adaptive masks", style = MaterialTheme.typography.titleMedium)
    HorizontalMultiBrowseCarousel(state, preferredItemWidth = 196.dp,
        modifier = Modifier.width(if (narrow) 260.dp else 1200.dp).height(220.dp).testTag("carousel"), itemSpacing = 8.dp) { index ->
        Column(Modifier.fillMaxWidth().height(220.dp).maskClip(MaterialTheme.shapes.extraLarge)) {
            Image(painterResource(R.drawable.landscape), "Landscape ${index + 1}", Modifier.fillMaxWidth().weight(1f), contentScale = ContentScale.Crop)
            Text("Landscape ${index + 1}\nImage ${index + 1}", Modifier.fillMaxWidth().background(MaterialTheme.colorScheme.surfaceContainer).padding(10.dp), style = MaterialTheme.typography.bodySmall)
        }
    }
}

@Composable
private fun NavigationScene() = SceneColumn {
    var selected by remember { mutableIntStateOf(2) }
    Text("Content navigation", style = MaterialTheme.typography.titleMedium)
    NavigationBar(Modifier.testTag("navigation")) {
        listOf("Home", "Library", "Activity", "Topics").forEachIndexed { index, label ->
            NavigationBarItem(selected = selected == index, onClick = { selected = index }, icon = {
                BadgedBox(badge = { if (index == 1) Badge { Text("3") } else if (index == 2) Badge() }) { Icon(when(index) { 0 -> Icons.Default.Home; 1 -> Icons.Default.Book; else -> Icons.Default.Star }, label) }
            }, label = { Text(label) }, modifier = Modifier.testTag("navigation-$index"))
        }
    }
    PrimaryTabRow(selectedTabIndex = selected.coerceAtMost(2), modifier = Modifier.testTag("tabs")) {
        listOf("Home", "Library", "Activity").forEachIndexed { index, label -> Tab(selected = selected == index, onClick = { selected = index }, text = { Text(label) }) }
    }
}

@Composable
private fun OverlaysScene() = SceneColumn {
    var dialog by remember { mutableStateOf(false) }
    var sheet by remember { mutableStateOf(false) }
    var menu by remember { mutableStateOf(false) }
    val context = LocalContext.current
    Box {
        Button(onClick = { menu = true }, modifier = Modifier.testTag("open-menu")) { Text("Menu") }
        DropdownMenu(menu, { menu = false }) { listOf("Edit", "Share", "Save").forEach { label -> DropdownMenuItem(text = { Text(label) }, onClick = { menu = false }) } }
    }
    Button(onClick = { dialog = true }, modifier = Modifier.testTag("open-dialog")) { Text("Dialog") }
    Button(onClick = { sheet = true }, modifier = Modifier.testTag("open-sheet")) { Text("Bottom sheet") }
    Button(onClick = {
        val native = SideSheetDialog(context)
        val content = LinearLayout(context).apply {
            orientation = LinearLayout.VERTICAL
            setPadding(24, 24, 24, 24)
            addView(TextView(context).apply { text = "Side sheet · MDC Android 1.14.0"; textSize = 24f })
            addView(TextView(context).apply { text = "Standard side information\nEditable draft\nIndependent content"; textSize = 18f })
        }
        native.setContentView(content)
        native.show()
    }, modifier = Modifier.testTag("open-side-sheet")) { Text("Side sheet · MDC Android") }
    TooltipBox(positionProvider = TooltipDefaults.rememberTooltipPositionProvider(TooltipAnchorPosition.Above), tooltip = { PlainTooltip { Text("Save draft") } }, state = rememberTooltipState()) {
        IconButton(onClick = {}, modifier = Modifier.testTag("tooltip")) { Icon(Icons.Default.Save, "Save") }
    }
    if (dialog) AlertDialog(onDismissRequest = { dialog = false }, title = { Text("Edit draft") }, text = { Text("Apply changes?") }, confirmButton = { TextButton(onClick = { dialog = false }) { Text("OK") } }, dismissButton = { TextButton(onClick = { dialog = false }) { Text("Cancel") } })
    if (sheet) ModalBottomSheet(onDismissRequest = { sheet = false }) { Text("Bottom sheet", Modifier.padding(24.dp), style = MaterialTheme.typography.headlineSmall); Text("Official default handle and transitions", Modifier.padding(24.dp)); Spacer(Modifier.height(240.dp)) }
}
