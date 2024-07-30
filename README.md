# **Engine4D**

A Unity toolkit for making 4D games.

Devlog series: [YouTube Playlist](https://youtube.com/playlist?list=PLh9DXIT3m6N4GygehtlHl0ukgrgPJZteI&si=WH-4ttQo6ApJ1UxV)

# Overview

Engine4D is a set of scripts, tools, and extensions to Unity for developing 4D (or even 5D) games. This engine is the core of [4D Golf](https://store.steampowered.com/app/2147950/4D\_Golf/) and contains everything you need to create your own higher dimensional games in Unity, minus all the golf-related assets. This documentation is still a work in progress, but goes over the main details you need to get started and work with Engine4D. The specific instructions here are mainly focused on 4D, but usually the same concepts apply to 5D by just replacing the 4D scripts and components with the 5D equivalents.

While this engine is in active development, the primary focus is with supporting features and compatibility for 4D Golf. So adding new features and improvements to the engine will be on a very strict case-by-case basis.

# Scene Requirements

Engine4D comes with some example scenes in the `Sample` folder. But if you’re starting from scratch, here are the requirements to build a 4D scene.

A 4D scene requires a special set of cameras in order to render the 4D slice view, the 4D ghost projections, traditional 2D/3D UI overlays, and other special effects. Simply add the `MainCamera` prefab to your scene to get all this functionality in one step.

If you would like to add additional overlays such as the Compass and Volume Line, you can also add the `UICanvas` prefab to the scene to get access to these features.

If your game is going to use physics colliders, they need to be initialized when the scene starts and updated if objects get removed or added during gameplay. This type of behavior is not automatically handled by the engine. You can use the `BuildColliders` script from the sample as a simple way to initialize the colliders when your scene starts.

To handle the lighting and the built-in skybox, there are several global shader variables that need to be initialized. The easiest place to start is using the `SetSkyColors` script from the sample. This script sets the skybox colors, the sun colors, and the directional lighting angles.

Lastly, there needs to be a camera controller to manage all of the cameras and setup rendering for the scene. At a minimum, you can inherit the bare-bones `BasicCamera4D` script to make your own player controller. However, you could also start from the premade `CameraControl4D` class which already takes care of player movement, rotation, physics, and collisions. The sample script `SamplePlayer4D` goes even further to add jumping controls. In the script, you can control the player's position with `position4D` and the camera's orientation with `camMatrix`.

# Important Editor Addons

Several editor addons are included to make working in 4D much easier. There are a few quirks and annoyances but these are generally just limitations of Unity doing what it wasn’t designed to do.

### Editor Volume Mode

Volume mode can be toggled in the editor using the `F10` key on the keyboard. Note that Unity generally loses track of the state when scripts reload, so you may need to press `F10` a few times to get everything rendering properly again.

### Editor Slicer

The Editor Slicer is a window that allows you to scan through the w coordinate being sliced (or y in the case of volume view). Open it from the top menu `4D > Slicer Window…` and mount it somewhere on your Unity panel, I like to put it above ‘Inspector’. Note that this tool has no way of knowing if you’re working in a 4D or 5D scene so you will have to manually check the `Use 5D` checkbox when working in 5D. Otherwise things won’t render correctly.

# Adding 4D Objects

Adding a 4D object to a scene is simple. There’s an editor plugin that does it automatically in the `GameObjects` menu (or by right-clicking in the empty space). You’ll find the menu item  `Create Object 4D` right under `Create Empty`. Otherwise, an object can be created manually by adding all the necessary components. 4D objects will generally have the following components:

### Object4D (always required)

This `Object4D` component adds additional properties to a Unity GameObject to make it 4 dimensional. This includes adding new dimensions for position, rotation, and scaling. All objects that need a 4D transform must have this component, including any parents or children of a 4D object. Engine4D does not support re-parenting during runtime, so all new objects and prefabs must be spawned in the scene's scope without a parent.

Setting an object’s position and scale in 4D is done using Unity’s Transform component for the x, y, and z components plus the extra fields `PositionW` and `scaleW` from `Object4D`. These can all be modified real-time during gameplay or in the editor.

Rotation works slightly differently. In the editor, Unity’s Euler angles from the Transform component rotate the object normaly, but there's an additional isoclinic set of Euler angles in the Object4D component called `DualEuler`. Together, these 6 Euler angles describe a full 4D rotation. You will see a preview of the rotation in the editor while changing any of the Euler angles.

However, during gameplay these angles are only used **once** when the object is created. That rotation is moved to the `localRotation4D` variable and that’s what gets used from that point on. So adjusting the Transform’s rotation or dual euler angles will have no effect after that point. If you want to directly initialize the matrix instead of using Euler angles when the object is created, you can check the field `MatrixRotationOnly` and fill in the local rotation matrix manually.

### ShadowFilter (required if MeshRenderer added)

If you have a `MeshRenderer` component on the 4D game object, you must also include the `ShadowFilter` component. The ShadowFilter controls which mesh renders to different cameras depending on if it’s a slice, ghost projection, or wireframe camera.

The triangle-based ghost projection mesh should be attached to the `ShadowMesh` field, or None if you don’t want a ghost. These meshes should always end with `_s`. The line-based wireframe projection gets attached to `WireMesh` or None if you don’t want a wireframe projection. These meshes should always end with `_w`.

Note that this component should be added even if you aren’t using any ghost projections because it’s still necessary to filter out the object from rendering to the wrong cameras. You also don’t need to use the same mesh for the ghost as the object. For example, you might use a high-poly “glome” in the mesh renderer, but a “600cell\_s” for the shadow to improve performance since the ghost detail isn’t as important.

### MeshRenderer

Attach the standard 4D tetrahedral mesh to the `MeshRender` component. This will be the one that doesn’t have `_s` or `_w` at the end of the mesh name. This is required if this object needs to be rendered to the screen. A 4D material is also required (see “Shaders and Materials” below).

### Colliders

There are many colliders to choose from in the `Scripts/Colliders` folder. Please note that all parameters, sizes, and coordinates of the colliders are in the mesh’s coordinate space, not the local transformed space. So any rotation, translation, or scale you make to the `Object4D` automatically gets applied to the collider.

### Occlusion4D (Optional)

The `Occlusion4D` class enables frustum culling on the object so that it doesn’t render when the camera is not looking at it or it’s not in the slice. This is a useful performance optimization for most medium and large meshes. There is some cost for performing this optimization so it may not provide benefits for small meshes. Occlusion4D computes the bounding sphere offline based on the current mesh when the component was added. If you change the mesh, you must right-click and “Reset” this component to re-compute the bounds.

# Physics

Engine 4D cannot use any built-in physics from Unity. Instead, Objects that need physics interactions such as gravity, velocity, and collisions should inherit from the `Physical4D` class. For maximum flexibility, the class does not perform the physics updates automatically, but instead provides the `UpdatePhysics()` method to be called by the derived class. This allows you to add other code before or after the physics update, call it in Update or FixedUpdate (FixedUpdate is recommended), or conditionally disable it or change properties. Physical objects have these tunable parameters:

* `velocityDecay` \- Friction or air resistance parameter defined by the number of seconds it would take to slow down to half the current velocity.
* `collisionsEnabled` \- Whether or not collision detection and intersection solving should run for this object during the physics updates.
* `colliderRadius` \- The radius for the sphere of collision. Only spherical colliders are supported by physics objects.
* `elastic` \- If true, the object can bounce when it collides with a surface.
* `limitSlope` \- The minimum cosine of the ground’s slope that can be walked on before sliding. 0 means all slopes can be walked on, 1 means only perfectly flat slopes.
* `restitution` \- When `elastic` is enabled, this 0 to 1 number represents the proportion of velocity lost in the bounce after the collision.
* `extendedRange` \- This is an artifact from 4D Golf where some colliders are marked to only apply to the player and not the ball. You probably shouldn’t use this.

In addition to the tunable properties in the inspector, there are several other useful variables that can be adjusted or used in your derived class including:

* `GRAVITY` \- A global static gravity strength for all physics objects.
* `gravityDirection` \- A local direction of gravity for this object.
* `useGravity` \- Enables or disables gravity for this object.
* `isGrounded` \- True if the object is on a walkable surface this frame.
* `lastHit` \- List of colliders that collided with the object in this frame.

# Modeling in 4D

### Loading, Editing, and Saving Meshes

Engine4D comes with some 4D meshes, but you can generate more yourself by using the editor menu `4D > Generate 4D Meshes`. The example code that generates these meshes is located in `Editor/GenrateMeshes4D.cs` and you can use that as a starting point to add more of your own models.

`GenrateMeshes4D` has a collection of Utilities for generating and loading meshes, each returning a `Mesh4DBuilder` object, which has further post-processing operations. While there are many to choose from, here are some of the most common mesh generators:

* `Generate4DFlat()` \- Starting from a 3D mesh, create a flat 3D surface in 4 dimensions. Mesh must be star-convex for this to be successful.
* `Generate4DExtrude()` \- Extrude a 3D mesh into 4D. Has options for adding or omitting the caps on top and bottom. Caps require the mesh to be star-convex.
* `Generate4DPyramid()` \- Extrude a 3D mesh into 4D as a pyramid. Has options for adding or omitting the cap. A cap requires the mesh to be star-convex.
* `Generate4DTruncatedPyramid()` \- Same as above but truncated at the top.
* `GenerateRevolve()` \- Revolve a 3D shape about the ZW plane to create a 4D surface of revolution. Supports full or partial rotations.
* `GeneratePathExtrude()` \- An extrusion along multiple segments stretched to match the shape of a given path. Similar to the surface of revolution, but more powerful.
* `GenerateDuoPrism()` \- Creates a 4D mesh from the product of two 2D surfaces.
* `OFFParser.LoadOFF4D()` \- Load a mesh from a .OFF file ([miratope](https://github.com/galoomba1/miratope-rs/releases/) export). The only supported cell shapes right now are tetrahedra, tetrahedral-bipyramids, and octahedra.
* `FDOParser.Load4DO()` \- Load a mesh from a .4DO file ([4DO-Specification](https://github.com/HoxelDraw/4DO-Specification)). Only some formats are currently supported.

Once you get your `Mesh4DBuilder` object, there are many things you can do with it, including chaining multiple operations together, such as:

* `Build()` \- Save the mesh and ghost projections to the `Meshes4D` folder.
* `Smoothen()` \- Compute smooth normals for the mesh (instead of default flat shading).
* `MergeVerts()` \- Smoothen() requires tetrahedra that share vertices to have **identical** vertex locations. Due to precision issues with some operations, you often need to merge similar vertices together first.
* `FlipNormals()` \- Flips the normals of the mesh (inside-out).
* `Perturb()` \- Adds random noise to the vertex positions.
* `GeoPoke()` \- Applies subdivision. Generally used to make geodesic spheres.
* `Spike()` \- Extrudes tetrahedral pyramids from all tetrahedral cells.
* `Translate()` \- Apply a positional shift.
* `Rotate()` \- Apply a rotation about a plane.
* `Scale()` \- Apply a uniform or non-uniform scale.
* `Affine()` \- Apply any affine transformation.
* `Homographic()` \- Apply any homographic transformation.

### In-Editor Modeling

Once you have models and building-block components, you may want to create more complicated, hand-designed meshes. While you could render a ton of different meshes seperately in your scene, it’s more optimized and draw-call efficient to combine the meshes into a single mesh. To do that, follow these steps:

Create a Unity prefab in the `Modeling` folder. Add 4D game objects to your scene and populate them with the correct meshes, ghost projections, and materials. You can nest/parent objects and apply transformations just like regular game objects, so long as everything has an `Object4D` component. Note that every unique material will become a separate submesh in the final merge at the end, so it’s important to use as few materials as possible.

Once you’re happy with your model, you can use the mesh generator `MergeMeshes4D()` inside of `GenrateMeshes4D` to create the combined mesh. Don’t forget to build it and then generate the new mesh.

### Fully Procedural Modeling

If you want to produce a mesh entirely from scratch by defining vertex positions from a script, there are tools for that in the `Mesh4D` class. The steps are the following:

Create a new `Mesh4D()` class with the number of sub-meshes to create as the argument. Each submesh can only have one material, so if you’d like to have an object with multiple materials, you’ll need at least one submesh per material. Then you can add tetrahedral cells with any of the following methods below. There's many variations of each of these related to normal vectors and vertex lighting.

* `AddTetrahedron()` \- Add a tetrahedron cell by directly specifying the 4 vertex coordinates.
* `AddCell()` \- Adds a cubic cell by specifying 8 vertices. This does not need to be an actual cube, but any convex warping of a cube is okay (for example a parallelepiped).
* `AddHalfCell()` \- Adds a triangular prism cell by specifying 6 vertices.
* `AddPyramid()` \- Adds a square pyramid cell by specifying the 5 vertices.

The methods above do not automatically add ghost projection meshes or wireframes to the mesh. To get those, you can call any of the methods below. Wireframes are automatically generated from the ghost projections so you don’t need to add those separately. Duplicate triangles and wireframes are automatically filtered out if they have identical or degenerate vertex positions.

* `AddTraingleShadow()` \- Adds a triangle to the ghost projection and wireframe.
* `AddQuadShadow()` \- Adds a quad to the ghost projection and wireframe. This omits the unnecessary diagonal line from the wireframe, so better to use this when possible.
* `AddCellShadow()` \- Adds a ghost projection of an entire cell. Equivalent to 6 quads.

Once the mesh is ready, it can be wrapped in a `Mesh4DBuilder` and generated just like the other modeling techniques.

# Shaders and Materials

### Basic Materials

There are several shaders included with Engine4D, the most basic being `DiffuseND.shader`. This shader, along with most custom ones, will include the following attributes in the material:

* `_Color` \- Defines the base color of the texture in RGB components. The alpha component is used to control the opacity of the ghost projection. Meshes with lots of overlapping ghost triangles should use a very low alpha, while meshes with very few ghost triangles can use a higher alpha value.
* `_ShadowDist` \- The maximum distance a ghost projection can still be rendered. It will smoothly fade out as it gets closer to this distance.
* `_Ambient` \- The amount of ambient lighting for the material (0 to 1).
* `_SpecularMul` \- How much the sky’s light gets reflected like a mirror (0 to 1).
* `_SpecularPow` \- How strong the sun’s specular highlight is (0 to 1).

### Global Shader Variables

The shader core uses a collection of global shader variables to affect the lighting of the entire scene. Some are updated automatically from various components of the engine, others can be manually controlled. These include:

* `_DitherDist` \- Surfaces with a depth less than this distance to the camera that have the `USE_DITHER` flag set in the shader will start to dither.
* `_DitherRadius` \- Surfaces less than this radius from the camera that have the `USE_DITHER` flag set in the shader will also start to dither.
* `_FogLevel` \- Inverse of the maximum render distance that would be fully fog.
* `_ShadowColor1` \- Used for ghost tinting in 4D. Alpha controls the strength of the tint.
* `_ShadowColor2` \- Used for ghost tinting in 4D.
* `_ShadowColor3` \- Used for ghost tinting in 5D.

There are also additional variables that can be found in `SetSkyColors.cs` to change the color of the sky, the color of the sun, and the angle of the directional light.

### Custom Shaders

All shaders must “inherit” from `CoreND.gcinc` by including it in order for them to render any 4D geometry. A good template for making a new custom shader is the `CheckerND.shader` shader. The CoreND will handle most of the heavy lifting, and you only need to define the following sections in this order:

* List of `#pragma shader_features` or `multi_compile`.
* List of `#define` attributes for CoreND.
* Define custom instanced properties if needed.
* A definition for `apply_proc_tex4D()` and `apply_proc_tex5D()` (may be empty).
* `#include_with_pragmas "CoreND.cginc"` (required).
* `CustomEditor "GeneralEditor"` (optional).

### Defines and Pragmas

These values can generally be either compile-time flags with `#pragma shader_feature`, run-time flags with `#pragma multi_compile`, or hard coded with `#define`. Common options include:

* `PROC_TEXTURE` \- Must be defined if you’re using a custom texturing algorithm. Default is to use the `_Color` parameter from the material for the texture.
* `PROC_VERT` \- Must be defined if you’re using a procedural vertex algorithm, for example to wiggle vertices in the wind.
* `LOCAL_UV` \- If defined, uv coordinates will be relative to the object’s transform. By default, uv coordinates represent world coordinates.
* `SKIP_SPECULAR` \- If defined, skips specular reflections and highlights.
* `USE_DITHER` \- Creates a dither pattern when the camera is too close to the object. Camera distance must be set by a script (not included in the samples).
* `FORCE_DITHER` \- Define a value to always use for dithering if `USE_DITHER` is enabled.
* `SPEC_POWER` \- Define a number for the specular power. Default is 20\.
* `VERTEX_AO` \- If defined, an additional vertex color property from the mesh is used to multiply with the color to create vertex lighting for ambient occlusion.
* `VERTEX_AO_SKIP_MUL` \- If defined, uses the property from above but skips the multiplication. This is for custom textures to use vertex color for other things.
* `CELL_AO` \- If defined, a per-vertex cell coordinate is added so it can be used in a custom texture. Cannot be used with `VERTEX_AO`.
* `FOG` \- If defined, the texture will blend into the sky color with distance falloff dependent on the `_FogLevel` global shader attribute.
* `SHADOW` \- If defined, the mesh is only a Ghost projection. Use the \_s mesh instead of the tetrahedralized mesh in the mesh renderer.
* `DOUBLE_SIDED_N` \- If defined, normals will be double sided. A side facing away from you will have the correct lighting compared to one facing you. This only makes sense to add if you’ve disabled culling with `Cull Off`.
* `DIFFUSE_COLOR` \- Define an alternative color for diffuse lighting.

### Custom Texturing

To create a custom texturing, define `PROC_TEXTURE` and define `apply_proc_tex4D()` with your texture function. The goal is to modify the local variable `color` to be the desired base texture color. Lighting effects are then applied automatically to the base color to produce the final color. Inside this function, there are variables you can use or modify for the computation including:

* `color` \- Besides being the output, it is initialized to the base color from `_Color`.
* `n` \- The normal vector.
* `i.uv` \- The uv coordinate which may be local or global depending on `LOCAL_UV`.
* `i.ao` \- The vertex lighting variable which may be useful if using `VERTEX_AO_SKIP_MUL` or `CELL_AO`.
* `i.viewDir` \- The direction and distance of the fragment relative to the camera.
* `_CamPosition` \- World position of the camera.

### Custom Vertex Manipulation

To manipulate vertices, define `PROC_VERT` and define `apply_proc_vert4D_init()` with your initialization code if needed, and `apply_proc_vert4D(v)` to adjust the position of the vertex `v`.

# Particle System

Engine4D comes with a basic particle system `ParticleSystem4D`. To use it, make sure the object has an `Object4D` component already, but **not** a `MeshRenderer`. Rendering is handled by the particle system. The shape of particle spawning volume is by default a unit sphere with the location and radius based on the position and scale of the particle spawner itself. Here are the parameters:

* `Max Spawn` \- Maximum particles that can be spawned.
* `Lifetime` \- Particle is killed after this time in seconds.
* `Spawn Rate` \- Number of particles to spawn per second.
* `Kill Below Y` \- Particle is killed if position's Y value is below this.
* `Prewarm Time` \- Number of seconds to prewarm the particle system.
* `Prewarm Delta Time` \- Deltatime to use during prewarm.
* `Min Velocity` & `Max Velocity` \- Randomly interpolated to get spawning velocity.
* `Accel` \- Acceleration particle experiences during lifetime.
* `Velocity Jitter` \- Amount of jittering force to apply.
* `Friction` \- Inverse of seconds to reach half velocity.
* `Follow Camera View` \- Spawning location follows the camera position.
* `Rand Color A` & `Rand Color B` \- Randomly interpolated to get `_Color` in the shader.
* `Fade In Time` \- Time in seconds to fade in the alpha value of `_Color` after spawning.
* `Fade Out Time` \- Time in seconds to fade out the alpha value of `_Color` based on lifetime.
* `Overlay` \- Render to the overlay camera instead of the main camera.
* `Min Scale` & `Max Scale` \- Randomly interpolated to get the spawning scale.
* `Scale Rate` \- The delta scale rate in units per second over lifetime.
* `Scale Axes` \- Non-uniform scale to apply to the particles.
* `Mesh` \- Mesh to use for the particle.
* `Billboard` \- Particle gets rotated to face the camera.
* `Force Slice Plane` \- Forces the particle to project onto the camera's slice plane.
* `Is Shadow` \- Mesh is a ghost projection mesh instead of a tetrahedral one.
* `Check Occlusion` \- Frustum culling for particles. Don't use with force slice plane.
* `Age Ordering` \- Sort particles to render from newest to oldest.
* `Mesh Materials` \- List of materials to use for particles (one per submesh).

# Advanced Editor Scripts

### Compile Templates

Sometimes you’ll want to write code that works in 4D and 5D. In that case, you can create a template in the `Templates` folder and add its name to `CompileTemplates.cs`. Then you’ll be able to create both versions when you use the editor menu `4D > Compile Templates`. The format of a template file is basically just a word substitution with the following keywords:

| Keyword | 4D Replacement | 5D Replacement |
| ----- | ----- | ----- |
| `VECTOR` | `Vector4` | `Vector5` |
| `SUBVECTOR` | `Vector3` | `Vector4` |
| `MATRIX` | `Matrix4x4` | `Matrix5x5` |
| `QTYPE` | `Quaternion` | `Isocline` |
| `LAST` | `w` | `v` |
| `DIMS` | `4` | `5` |
| `<D>` | `4D` | `5D` |
| `<D-1>` | `3D` | `4D` |

### Generate 3D Textures

Unlike other assets in the engine, the example textures are not included due to their large size. You can generate them yourself by using `4D > Generate Textures` (this may take a few minutes). These 3D textures can be used with various shaders to create materials for your 4D Game. You can create your own custom textures using the tools I've provided in the `GenerateTextures.cs` script.

### Generate Groups

Generates a list of symmetry groups from polytopes to make polytope compounds with `GenerateCompound()` in the `GenerateMeshes4D.cs` script. Basic groups come prepackaged in Engine4D so it’s unlikely you’ll need this.

### Generate Slice Look-up Table

You won’t need to run this since the lookup tables are included in Engine4D. But I’ve left the code that generates them for study. Use `4D > Generate LUTs` to regenerate.

# Utilities

### Math

`Vector5` and `Matrix5x5` are designed to mimic Unity’s `Vector4` and `Matrix4x4` implementing identical methods. `Transform4D` and `Transform5D` represent a full transformation similar to Unity’s `Transform` class, but they also contain a large number of static math utilities that are useful in higher dimensions.

The `Isocline` is sort of a “quaternion” equivalent for 4 dimensions, representing a full 4D rotation as a pair of 2 isoclinic rotations. They’re useful in the same way normal quaternions are to 3D.

There is also the `AxisRotation` struct which is the 4D equivalent of the “Axis-Angle” representation in 3D. The difference is that the axis is now 4-dimensional (represented as a quaternion) and instead of the rotation component being an angle on a plane, the rotation is a 3D rotation of a space (another quaternion).

All of these classes and functions are verified with Unit tests in `Tests4D.cs` and `Tests5D.cs`.

### Random

`PseudoRandom` is a class that adds additional pseudo-random number generation for multiple dimensions. The seed can be set manually with `_seed`. Functions include:

* `UniformND` \- Uniform random vectors for up to 5 dimensions.
* `NormalND` \- Normally distributed vectors for up to 5 dimensions.
* `SphereND` \- Uniformly random samples from the surface of an N-ball.
* `BallND` \- Uniformly random samples from the interior of an N-ball.
* `RotationND` \- Uniformly random rotation in N dimensions.
* `RotationMatrixND` \- Uniformly random rotation matrix in N dimensions.

### Other

`InputManager` is a static class to handle inputs, allowing runtime re-binding, joystick support, input pausing, sensitivity and invert controls, and so on. This is mostly an artifact from 4D Golf, but it’s included to help with the examples.

`WavUtil` is a class for saving and loading .wav files. It’s another artifact from a 4D Golf editor script and is only included incidentally.

`Singleton` is a simple class to use the same global gameobject across all scenes.

# VR and XR

This is still a work in progress. More documentation will come when official VR support is finished.
