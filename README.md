# Replicator Plugin

This unofficial TaleSpire plugin that allows you to replicate custom asset over a line of one or more line segements
or within the area of a circle. The replication has a (by default) hidden base which can be used to move the replication
after it has been created. Can use an existing Mini transformed asset as a source or the content to be replicated can be
prompted for.

Special thanks to AlbrechtWM for writing most of the code for extending the plugin to support the circle area fill via
the sphere ruler.

(Preview show old static FlameWall. New WallOfFlame is a beautiful particle system effect solution instead)

![Preview](https://i.imgur.com/xdIcEki.png)

Video: https://www.youtube.com/watch?v=cx7Tfsl7xfo

## Change Log

1.2.1: Fixed compatibility issue after TS update
1.2.1: Optimized new base detection
1.2.0: Plugin now supports both line and circle area (via sphere ruler) replication
1.2.0: Selected ruler type (line or circle) is indciated when plugin is active
1.2.0: Replicated based now has a friendly name
1.2.0: Includes blank asset for making source minis (e.g. mini transformation to blank and then add desired effect)
1.1.1: Updated WallOfFire asset to multiple heights called by WallOfFire, WallOfFire.20 and WallOfFire.30
1.1.0: Modified name of replicated objects to allow Grab/Drop plugin to properly handle delete of Replicated content
1.0.2: Cheap static FlameWall replaced by WallOfFlame
1.0.1: Fixed image reference (no plugin change)
1.0.0: Initial release

## Install

Use R2ModMan or similar installer to install this plugin.

## Usage

Press the keyboard shortcut (default LCTRL+F for Fill) to activate the Replicator. A line at the top of the screen
will indicate that Replication has been activated and indicate if a mini effect asset was selected to use as the source
or if the content name will be prompted. Next open the measuring tool (defaul m) and switch to the line version (default 2)
or sphere version (default 3). Use the line or sphere measurement tool to create a line consisting of one or more line
segments or define the center and radius of a circle. Right click to end line segment creation and right click a second time
to clear the defined line. If a mini with an effect was not selected before starting the Replicator function then a prompt will
appear asking for the name of the content to be replicated. If a mini with an effect was selected before starting the Replicator
function then that custom effect content will be replicated instead and no prompt is displayed. The specified custom content is
copied along the specified line with roughly one copy per tile.

The replicated line has a single base at the start of the line but it is hidden by default. If the line needs to be moved
the GM can used the Hide/Unhide to unhide the base or can move the base while in the GM view.

A sample custom asset is provide with this plugin: WallOfFire

### Using Effect Content From A Mini

To avoid having to type the content name, it is possible to use an existing mini as the source of the replication. If a mini
is selected prior to using the Replication function and the mini has an (CMP) effect applied to it, the effect will be used
as the source of the replicaton. Please use the CMP Effect option (not CMP Scaled Effect or CMP Temporary Effect) to create
such source minis.

Since CMP Effects do not remove the original mini appearance, if it is desirable to just show the effect without the original
mini use the CMP Mini Transfromation to transform the mini to the "blank" asset (provided with this plugin). This will leave
only the base but remove the original mini mesh. Now you can apply the desired effect. This will make the base selectable by
players (for replication) without showing the original mini mesh.

## Limitations

1. Only assetBundle content is compatible with this plugin.
2. Only custom content can be replicated at this time. Custom content must follow the rules outline for compatability with
   the Custom Mini Plugin (i.e. be present in a sub-folder named after the content name, with a file following the content
   name and contain a prefab following the content name).
3. The resulting (by default hidden) base can be used to move the line but, at the moment, not rotate it.

# Special Thanks

Thanks to Sirhaian'Arts YouTube Video for providing the base knowledge for making the Wall Of Fire.
