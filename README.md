# Replicator Plugin

This unofficial TaleSpire plugin that allows you to replicate custom asset over a line of one or more line segements.
The line has a (by default) hidden base which can be used to move the replication after it has been created. Can use
an existing Mini transformed asset as a source or the content to be replicated can be prompted for.

![Preview](https://imgur.com/a/kUdp3UX)

## Change Log

1.0.0: Initial release

## Install

Use R2ModMan or similar installer to install this plugin.

## Usage

Press the keyboard shortcut (default LCTRL+F for Fill) to activate the (Line) Replicator. A line at the top of the screen
will indicate that Replication has been activated and indicate if a mini transformed asset was selected to use as the source
or if the content name will be prompted. Next open the measuring tool (defaul m) and switch to the line version (default 2).
Only the line measurement tool can be used with the Replicator plugin (i.e. do not use the cone or sphere tools). Use the
line measurement tool to create a line consisting of one or more line segments. Right click to end line segment creation and
right click a second time to clear the defined line. If a transformed mini was not selected before starting the Replicator
function then a prompt will appear asking for the name of the content to be replicated. If a transformed mini was selected
before starting the Replicator function then that custom content will be replicated instead and no prompt is displayed.
The specified custom content is copied along the specified line with roughly one copy per tile.

The replicated line has a single base at the start of the line but it is hidden by default. If the line needs to be moved
the GM can used the Hide/Unhide to unhide the base or can move the base while in the GM view.

A sample custom asset is provide with this plugin: FlameWall

## Limitations

1. Only assetBundle content is compatible with this plugin.
2. Only custom content can be replicated at this time. Custom content must follow the rules outline for compatability with
   the Custom Mini Plugin (i.e. be present in a sub-folder named after the content name, with a file following the content
   name and contain a prefab following the content name).
3. The resulting (by default hidden) base can be used to move the line but, at the moment, not rotate it.
