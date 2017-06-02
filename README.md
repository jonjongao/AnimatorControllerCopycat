# Animator Controller Copycat for Unity #

![](http://i.imgur.com/GkzCq34.png)

This tool is use for situation like you have a finished Animator Controller, you about to take this into mass production level. Bunch of character's animation clips need to swap into the Animator Controller with same construct.

So you start duplicating, click lots of Animator State, swap into new clip one by one.

If bad things happen like original construct is changed, and you have to do that all over again.

Animator Controller Copycat will help you saving precious time, what it does is mapping out template Animator Controller, give you Motion field. You just drag new clip into field and click Save. Done!

## Usage ##
- Import Animator Controller Copycat asset, uncheck Demo folder if you don't need it, It's UnityChan Animator Controller and animations.
- Start tool from Window/Animator Controller Copycat.
- Drag template Animator Controller to template field.
- Drag new animation clip to motion field which you want swapping to.
- Click Save button to save it. Done!

## Note ##
**Try not swap BlendTree field with AnimationClip, it'll cause BlendTreeInspector.cs error and sometimes leave those Motion which belong to that BlendTree stay exist.**
