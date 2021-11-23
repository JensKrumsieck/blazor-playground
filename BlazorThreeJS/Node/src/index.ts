import * as THREE from 'three'
import { Ray, Vector2 } from 'three';
import { OrbitControls } from 'three/examples/jsm/controls/OrbitControls.js'

let canvas: HTMLElement;
let renderer: THREE.WebGLRenderer;
let camera: THREE.OrthographicCamera;
let controls: OrbitControls;
let mouse = new THREE.Vector2();
let raycaster = new THREE.Raycaster();

const scene = new THREE.Scene()
const pointLight = new THREE.PointLight(0xffffff, 0.1);
pointLight.position.x = 2
pointLight.position.y = 3
pointLight.position.z = 4
scene.add(pointLight)

const update = () => {
    controls.update()
    renderer.render(scene, camera)
    window.requestAnimationFrame(update)
}

export function init(canvasSelector: string) {
    canvas = document.getElementById(canvasSelector);
    renderer = new THREE.WebGLRenderer({
        antialias: true,
        alpha: true,
        canvas: canvas
    })
    addCamera();
    //resize listener
    window.addEventListener('resize', () => updateCamera())
    //mouse listener
    window.addEventListener('mousemove', onMouseMove, false);
    //start animation & controls 
    update();
}

export function addAtom(x: number, y: number, z: number, radius: number, color: string)
{
    const material = new THREE.MeshBasicMaterial({
        color: new THREE.Color(color)
    })
    var atom = new THREE.SphereGeometry(radius / (77 * 2), 32, 32);
    var sphere = new THREE.Mesh(atom, material)
    sphere.position.set(x, y, z)
    scene.add(sphere)
}

export function addBond(x: number, y: number, z: number, qx: number, qy: number, qz: number, qw: number, length: number) {
    const material = new THREE.MeshBasicMaterial({
        color: new THREE.Color("#999")
    });
    var bond = new THREE.CylinderGeometry(.1, .1, length);
    var cylinder = new THREE.Mesh(bond, material);
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
    const sizes = getParentSize();
    camera = new THREE.OrthographicCamera(sizes.width / -2, sizes.width / 2, sizes.height / 2, sizes.height / -2, -100, 1000);
    camera.zoom = 15;
    camera.position.set(0, 0, -5)
    updateCamera();
    scene.add(camera)
    controls = new OrbitControls(camera, canvas)
}

function updateCamera() {
    var sizes = getParentSize();
    camera.left = sizes.width / -2;
    camera.right = sizes.width  / 2;
    camera.top = sizes.height / 2;
    camera.bottom = -sizes.height / 2;
    camera.updateProjectionMatrix()
    renderer.setSize(sizes.width, sizes.height)
    renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2))
}

function getParentSize() {
    var box = canvas.parentElement;
    return { width: box.clientWidth, height: box.clientHeight }
}

function onMouseMove(event) {
    //raycast script
    var rect = canvas.getBoundingClientRect();
    mouse.x = ((event.clientX - rect.left) / (rect.right - rect.left)) * 2 - 1;
    mouse.y = - ((event.clientY - rect.top) / (rect.bottom - rect.top)) * 2 + 1;

    raycaster.setFromCamera(mouse, camera);
    const intersects = raycaster.intersectObjects(scene.children, true);
    if (intersects.length > 0) {
        const near = intersects[0];
        (near.object as any).material.color.set(0xff0000);
    }
}