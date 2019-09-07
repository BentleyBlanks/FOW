# FOW

Some possible implementations of 'Fog of War'. Both of them based on screen space infomation to generate Fog of War, and both of them are runnable on mobile device.

## Introduction

>   Using Unity 2019.1.4f1, should worked in other version of Unity.

SDF's resolution recommend 128x128 or 256x256, and Grid is recommened 32x32 or 64x64

Implementation detail about these could be find [here]([https://github.com/BentleyBlanks/Notes/blob/master/notes/Renderer/Screen%20Space%20Fog%20of%20War.md](https://github.com/BentleyBlanks/Notes/blob/master/notes/Renderer/Screen Space Fog of War.md))



### SDF

![FinalResult-1](img/FinalResult-1.png)

Using JFA to real-time generating Signed Distance Field Texture, and RayMarching to calculate the 2d fog of war.

---

### Grid

![1564411522311](img/1564411522311.png)

Using CPU to rayCast a pregenerate mapTexture, and fill a FOW texture.



## Author

``` cpp
const char* 官某某 = "Bingo";

std::string 个人博客 = "http://bentleyblanks.github.io";
```