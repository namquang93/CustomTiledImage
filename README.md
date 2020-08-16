# Custom Tiled Image

## Intro
This is a small script that helps you to customize tiled image easier. Right now we can only have a tiled image to flip.

![Intro](CustomTiledImage.jpg?raw=true "Intro")

## How to use
Simply add the component to an UI gameobject, assign a sprite to it and then check Flip Horizontal When Tiled or Flip Vertical When Tiled.

## How it works
**AlleyLabs.Engine.CustomTiledImage** extends **UnityEngine.UI.Image** and override the method **OnPopulateMesh** to change the behavior when the image is tiled. The trick is to swap the uvMin and uvMax when the tile index is odd.

```
if (flipH) {
    var tmp = uvMin.x;
    uvMin.x = uvMax.x;
    uvMax.x = tmp;
}
 
if (flipV) {
    var tmp = uvMin.y;
    uvMin.y = uvMax.y;
    uvMax.y = tmp;
}
```

## License
This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details