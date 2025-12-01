import { Component, ElementRef, OnInit, AfterViewInit, OnDestroy, ViewChild, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import * as THREE from 'three';

@Component({
  selector: 'app-warehouse-animation',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './warehouse-animation.component.html',
  styleUrls: ['./warehouse-animation.component.scss']
})
export class WarehouseAnimationComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('rendererContainer', { static: true }) rendererContainer!: ElementRef;

  private scene!: THREE.Scene;
  private camera!: THREE.PerspectiveCamera;
  private renderer!: THREE.WebGLRenderer;
  private animationId!: number;
  
  // Scene Objects
  private particles!: THREE.Points;
  private rackSystem: THREE.Group = new THREE.Group();
  private agvs: THREE.Group = new THREE.Group();

  constructor(private ngZone: NgZone) {}

  ngOnInit(): void {}

  ngAfterViewInit(): void {
    this.initThree();
    this.createHolographicWarehouse();
    this.createColdParticles();
    this.animate();
    
    window.addEventListener('resize', this.onWindowResize.bind(this));
  }

  ngOnDestroy(): void {
    if (this.animationId) {
      cancelAnimationFrame(this.animationId);
    }
    window.removeEventListener('resize', this.onWindowResize.bind(this));
    
    if (this.renderer) {
      this.renderer.dispose();
    }
  }

  private initThree(): void {
    const width = this.rendererContainer.nativeElement.clientWidth;
    const height = this.rendererContainer.nativeElement.clientHeight;

    // 1. Scene Setup - Professional Deep Teal Background
    this.scene = new THREE.Scene();
    this.scene.background = new THREE.Color(0x001e26); // Very dark teal/black
    this.scene.fog = new THREE.FogExp2(0x001e26, 0.015);

    // 2. Camera Setup
    this.camera = new THREE.PerspectiveCamera(50, width / height, 0.1, 1000);
    this.camera.position.set(30, 25, 40);
    this.camera.lookAt(0, 0, 0);

    // 3. Renderer Setup
    this.renderer = new THREE.WebGLRenderer({ antialias: true, alpha: true });
    this.renderer.setSize(width, height);
    this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2)); // Cap pixel ratio for performance
    this.rendererContainer.nativeElement.appendChild(this.renderer.domElement);

    // 4. Lighting - Cold & Clinical
    const ambientLight = new THREE.AmbientLight(0x00bcd4, 0.4); // Cyan ambient
    this.scene.add(ambientLight);

    const mainLight = new THREE.DirectionalLight(0xffffff, 1);
    mainLight.position.set(10, 30, 20);
    this.scene.add(mainLight);

    // Accent lights (Cyan/Blue glow)
    const blueLight = new THREE.PointLight(0x00bcd4, 2, 50);
    blueLight.position.set(-15, 10, -15);
    this.scene.add(blueLight);

    const tealLight = new THREE.PointLight(0x006064, 2, 50);
    tealLight.position.set(15, 10, 15);
    this.scene.add(tealLight);
  }

  private createHolographicWarehouse(): void {
    // Floor Grid - Digital Twin Style
    const gridHelper = new THREE.GridHelper(100, 50, 0x00bcd4, 0x004d40);
    (gridHelper.material as THREE.Material).transparent = true;
    (gridHelper.material as THREE.Material).opacity = 0.3;
    this.scene.add(gridHelper);

    // Create Racks
    this.createRackRows();
    this.scene.add(this.rackSystem);

    // Create AGVs
    this.createAGVs();
    this.scene.add(this.agvs);
  }

  private createRackRows(): void {
    const rackGeometry = new THREE.BoxGeometry(1, 8, 12);
    const edgesGeometry = new THREE.EdgesGeometry(rackGeometry);
    
    // Holographic Material (Wireframe look)
    const rackMaterial = new THREE.LineBasicMaterial({ color: 0x00838f, transparent: true, opacity: 0.4 });
    const shelfMaterial = new THREE.MeshBasicMaterial({ color: 0x00bcd4, transparent: true, opacity: 0.1, side: THREE.DoubleSide });
    
    // Pallet Geometry
    const palletGeo = new THREE.BoxGeometry(0.9, 0.8, 0.9);
    const palletEdges = new THREE.EdgesGeometry(palletGeo);
    const palletLineMat = new THREE.LineBasicMaterial({ color: 0xffffff, transparent: true, opacity: 0.3 });

    for (let x = -20; x <= 20; x += 10) {
      for (let z = -20; z <= 20; z += 15) {
        // Rack Frame (Wireframe)
        const rack = new THREE.LineSegments(edgesGeometry, rackMaterial);
        rack.position.set(x, 4, z);
        this.rackSystem.add(rack);

        // Shelves & Pallets
        for(let y = 1; y < 8; y += 2) {
            // Shelf Plane
            const shelf = new THREE.Mesh(new THREE.PlaneGeometry(1, 12), shelfMaterial);
            shelf.rotation.x = -Math.PI / 2;
            shelf.position.set(x, y, z);
            this.rackSystem.add(shelf);

            // Random Pallets
            for(let pz = -5; pz <= 5; pz += 1.2) {
                if(Math.random() > 0.2) {
                    const pallet = new THREE.LineSegments(palletEdges, palletLineMat);
                    pallet.position.set(x, y + 0.45, z + pz);
                    this.rackSystem.add(pallet);
                    
                    // Inner "Glow" for pallet content
                    const innerBox = new THREE.Mesh(
                        new THREE.BoxGeometry(0.8, 0.7, 0.8),
                        new THREE.MeshBasicMaterial({ color: 0x00bcd4, transparent: true, opacity: 0.15 })
                    );
                    pallet.add(innerBox);
                }
            }
        }
      }
    }
  }

  private createAGVs(): void {
      const agvGeo = new THREE.BoxGeometry(1.2, 0.4, 1.8);
      const agvEdges = new THREE.EdgesGeometry(agvGeo);
      const agvMat = new THREE.LineBasicMaterial({ color: 0x00e5ff }); // Bright Cyan
      
      for(let i=0; i<6; i++) {
          const agv = new THREE.LineSegments(agvEdges, agvMat);
          
          // Add a "core" light
          const light = new THREE.PointLight(0x00e5ff, 1, 4);
          light.position.set(0, 0.5, 0);
          agv.add(light);

          // Initial Position
          agv.position.set(
              (Math.random() - 0.5) * 40,
              0.2,
              (Math.random() - 0.5) * 40
          );
          
          agv.userData = {
              speed: 0.08 + Math.random() * 0.05,
              direction: new THREE.Vector3(Math.random()-0.5, 0, Math.random()-0.5).normalize(),
              changeTime: 0
          };
          
          this.agvs.add(agv);
      }
  }

  private createColdParticles(): void {
    const particleCount = 1500;
    const geometry = new THREE.BufferGeometry();
    const positions = new Float32Array(particleCount * 3);
    const colors = new Float32Array(particleCount * 3);

    const color = new THREE.Color(0x00bcd4); // Cyan particles

    for (let i = 0; i < particleCount; i++) {
      positions[i * 3] = (Math.random() - 0.5) * 100; // x
      positions[i * 3 + 1] = Math.random() * 40;      // y
      positions[i * 3 + 2] = (Math.random() - 0.5) * 100; // z

      colors[i * 3] = color.r;
      colors[i * 3 + 1] = color.g;
      colors[i * 3 + 2] = color.b;
    }

    geometry.setAttribute('position', new THREE.BufferAttribute(positions, 3));
    geometry.setAttribute('color', new THREE.BufferAttribute(colors, 3));

    const material = new THREE.PointsMaterial({
      size: 0.15,
      vertexColors: true,
      transparent: true,
      opacity: 0.6,
      blending: THREE.AdditiveBlending
    });

    this.particles = new THREE.Points(geometry, material);
    this.scene.add(this.particles);
  }

  private animate(): void {
    this.ngZone.runOutsideAngular(() => {
      const loop = () => {
        this.animationId = requestAnimationFrame(loop);
        
        const time = Date.now() * 0.0005;

        // 1. Cinematic Camera Movement
        this.camera.position.x = Math.sin(time * 0.5) * 35;
        this.camera.position.z = Math.cos(time * 0.5) * 35;
        this.camera.lookAt(0, 5, 0);

        // 2. Animate Particles (Falling "Cold Air" effect)
        const positions = this.particles.geometry.attributes['position'].array as Float32Array;
        for(let i = 1; i < positions.length; i += 3) {
            positions[i] -= 0.05; // Move down
            if (positions[i] < 0) {
                positions[i] = 40; // Reset to top
            }
        }
        this.particles.geometry.attributes['position'].needsUpdate = true;

        // 3. Animate AGVs
        this.agvs.children.forEach(agv => {
            agv.position.add(agv.userData['direction'].clone().multiplyScalar(agv.userData['speed']));
            
            // Bounds check
            if(Math.abs(agv.position.x) > 25 || Math.abs(agv.position.z) > 25) {
                agv.userData['direction'].negate();
            }
            
            // Random turns
            if(Date.now() > agv.userData['changeTime']) {
                // Snap to 90 degree turns for "robotic" feel
                const axis = Math.random() > 0.5 ? new THREE.Vector3(1, 0, 0) : new THREE.Vector3(0, 0, 1);
                const sign = Math.random() > 0.5 ? 1 : -1;
                agv.userData['direction'] = axis.multiplyScalar(sign);
                
                agv.userData['changeTime'] = Date.now() + 1000 + Math.random() * 4000;
                agv.lookAt(agv.position.clone().add(agv.userData['direction']));
            }
        });

        this.renderer.render(this.scene, this.camera);
      };
      loop();
    });
  }

  private onWindowResize(): void {
    const width = this.rendererContainer.nativeElement.clientWidth;
    const height = this.rendererContainer.nativeElement.clientHeight;
    
    this.camera.aspect = width / height;
    this.camera.updateProjectionMatrix();
    this.renderer.setSize(width, height);
  }
}
