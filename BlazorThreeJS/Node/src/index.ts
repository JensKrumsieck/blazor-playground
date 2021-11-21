import * as THREE from 'three'
import { OrbitControls } from 'three/examples/jsm/controls/OrbitControls.js'
import * as dat from 'dat.gui'

let canvas: HTMLElement;
let camera, renderer;

// Scene
const scene = new THREE.Scene()

// Objects
const geometry = new THREE.TorusGeometry(.7, .2, 16, 100);

// Materials
const material = new THREE.MeshBasicMaterial()
material.color = new THREE.Color(0xff0000)

// Mesh
const sphere = new THREE.Mesh(geometry, material)
scene.add(sphere)
// Lights
const pointLight = new THREE.PointLight(0xffffff, 0.1)
pointLight.position.x = 2
pointLight.position.y = 3
pointLight.position.z = 4
scene.add(pointLight)

const clock = new THREE.Clock()

const tick = () => {

    const elapsedTime = clock.getElapsedTime()

    // Update objects
    sphere.rotation.y = .5 * elapsedTime

    // Update Orbital Controls
    // controls.update()

    // Render
    renderer.render(scene, camera)

    // Call tick again on the next frame
    window.requestAnimationFrame(tick)
}

export function init(canvasSelector: string) {
    // Debug
    let gui = new dat.GUI()

    // Canvas
    canvas = document.getElementById(canvasSelector);
    console.log(canvas);
    /**
     * Sizes
     */
    const sizes = {
        width: canvas.clientWidth,
        height: canvas.clientHeight
    }

    console.log(sizes);
    /**
     * Camera
     */
    // Base camera
    camera = new THREE.PerspectiveCamera(75, sizes.width / sizes.height, 0.1, 100)
    camera.position.x = 0
    camera.position.y = 0
    camera.position.z = 2
    scene.add(camera)

    // Controls
    //   controls = new OrbitControls(camera, canvas)
    // controls.enableDamping = true

    /**
     * Renderer
     */
    renderer = new THREE.WebGLRenderer({
        antialias: true,
        canvas: canvas
    })
    renderer.setSize(sizes.width, sizes.height)
    renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2))

    window.addEventListener('resize', () => {
        // Update sizes
        sizes.width = canvas.clientWidth
        sizes.height = canvas.clientHeight

        // Update camera
        camera.aspect = sizes.width / sizes.height
        camera.updateProjectionMatrix()

        // Update renderer
        renderer.setSize(sizes.width, sizes.height)
        renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2))
    })
    tick();
}