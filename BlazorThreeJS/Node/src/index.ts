import * as THREE from 'three'
import { OrbitControls } from 'three/examples/jsm/controls/OrbitControls.js'

let canvas: HTMLElement;
let camera, renderer, controls;
const scene = new THREE.Scene()
const pointLight = new THREE.PointLight(0xffffff, 0.1);
pointLight.position.x = 2
pointLight.position.y = 3
pointLight.position.z = 4
scene.add(pointLight)

const tick = () => {
    controls.update()
    renderer.render(scene, camera)
    window.requestAnimationFrame(tick)
}

export function init(canvasSelector: string) {
    canvas = document.getElementById(canvasSelector);
    const sizes = {
        width: canvas.clientWidth,
        height: canvas.clientHeight
    }
    addCamera();

    renderer = new THREE.WebGLRenderer({
        antialias: true,
        alpha: true,
        canvas: canvas
    })
    renderer.setSize(sizes.width, sizes.height)
    renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2))

    window.addEventListener('resize', () => {

        sizes.width = canvas.clientWidth
        sizes.height = canvas.clientHeight

        updateCamera(sizes);

        renderer.setSize(sizes.width, sizes.height)
        renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2))
    })
    tick();
}

export function addAtom(x: number, y: number, z: number, radius: number, color: string)
{
    const material = new THREE.MeshBasicMaterial({
        color: new THREE.Color(color)
    })
    var atom = new THREE.SphereGeometry(radius/(77*2), 32, 32);
    let sphere = new THREE.Mesh(atom, material)
    sphere.position.set(x, y, z)
    scene.add(sphere)
}

export function addBond(x: number, y: number, z: number, qx: number, qy: number, qz: number, qw: number, length: number) {
    const material = new THREE.MeshBasicMaterial({
        color: new THREE.Color("#999")
    });
    var bond = new THREE.CylinderGeometry(.1, .1, length);
    let cylinder = new THREE.Mesh(bond, material);
    cylinder.quaternion.set(qx, qy, qz, qw);
    cylinder.position.set(x, y, z)
    scene.add(cylinder);
}

export function clearCanvas() {
    if (canvas != undefined && canvas != null) {
        scene.clear();
        addCamera();
        scene.add(pointLight)
    }
}

function addCamera() {
    const sizes = {
        width: canvas.clientWidth,
        height: canvas.clientHeight
    }
    camera = new THREE.OrthographicCamera(sizes.width / -2, sizes.width / 2, sizes.height / 2, sizes.height / -2, -100, 1000);
    camera.zoom = 15;
    camera.position.set(0, 0, -5)
    updateCamera(sizes)
    scene.add(camera)
    controls = new OrbitControls(camera, canvas)
}

function updateCamera(sizes) {
    camera.aspect = sizes.width / sizes.height
    camera.updateProjectionMatrix()
}