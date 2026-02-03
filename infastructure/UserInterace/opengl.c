#include <GL/gl.h>
#include <GL/glu.h>
#include <GL/glut.h>
#include <stdio.h>
#include <stdlib.h>
#include <math.h>
#include <time.h>

// Camera structure
typedef struct {
    float x, y, z;
    float rx, ry;  // Rotation angles
    float fov;
} Camera;

// Object structure
typedef struct {
    float x, y, z;
    float rx, ry, rz;
    float scale;
    float color[3];
    int type;  // 0=cube, 1=sphere, 2=pyramid, 3=cylinder
} GLObject;

// Global variables
Camera camera = {0, 0, 5, 0, 0, 45.0f};
GLObject* objects = NULL;
int num_objects = 10;
int window_width = 800;
int window_height = 600;
float rotation_angle = 0.0f;
int show_axes = 1;
int show_grid = 1;
int wireframe = 0;
int lighting_enabled = 1;

// Function to initialize OpenGL
void init_opengl() {
    glClearColor(0.1f, 0.1f, 0.2f, 1.0f);
    glEnable(GL_DEPTH_TEST);
    glDepthFunc(GL_LESS);
    
    if (lighting_enabled) {
        glEnable(GL_LIGHTING);
        glEnable(GL_LIGHT0);
        glEnable(GL_COLOR_MATERIAL);
        glColorMaterial(GL_FRONT_AND_BACK, GL_AMBIENT_AND_DIFFUSE);
        
        // Set up light
        GLfloat light_pos[] = {5.0f, 5.0f, 5.0f, 1.0f};
        GLfloat light_ambient[] = {0.2f, 0.2f, 0.2f, 1.0f};
        GLfloat light_diffuse[] = {0.8f, 0.8f, 0.8f, 1.0f};
        
        glLightfv(GL_LIGHT0, GL_POSITION, light_pos);
        glLightfv(GL_LIGHT0, GL_AMBIENT, light_ambient);
        glLightfv(GL_LIGHT0, GL_DIFFUSE, light_diffuse);
    }
    
    glShadeModel(GL_SMOOTH);
}

// Function to create random objects
void create_random_objects() {
    if (objects) free(objects);
    
    objects = (GLObject*)malloc(num_objects * sizeof(GLObject));
    if (!objects) {
        fprintf(stderr, "Memory allocation failed\n");
        exit(1);
    }
    
    srand(time(NULL));
    
    for (int i = 0; i < num_objects; i++) {
        objects[i].x = (rand() % 200 - 100) / 10.0f;
        objects[i].y = (rand() % 200 - 100) / 10.0f;
        objects[i].z = (rand() % 200 - 100) / 10.0f;
        
        objects[i].rx = rand() % 360;
        objects[i].ry = rand() % 360;
        objects[i].rz = rand() % 360;
        
        objects[i].scale = (rand() % 50 + 50) / 100.0f;
        
        objects[i].color[0] = (rand() % 100) / 100.0f;
        objects[i].color[1] = (rand() % 100) / 100.0f;
        objects[i].color[2] = (rand() % 100) / 100.0f;
        
        objects[i].type = rand() % 4;  // 4 types of objects
    }
}

// Function to draw a cube
void draw_cube(float size, float* color) {
    glColor3fv(color);
    
    if (wireframe) {
        glPolygonMode(GL_FRONT_AND_BACK, GL_LINE);
        glLineWidth(2.0f);
    }
    
    float s = size / 2.0f;
    
    glBegin(GL_QUADS);
    // Front face
    glNormal3f(0.0f, 0.0f, 1.0f);
    glVertex3f(-s, -s, s);
    glVertex3f(s, -s, s);
    glVertex3f(s, s, s);
    glVertex3f(-s, s, s);
    
    // Back face
    glNormal3f(0.0f, 0.0f, -1.0f);
    glVertex3f(-s, -s, -s);
    glVertex3f(-s, s, -s);
    glVertex3f(s, s, -s);
    glVertex3f(s, -s, -s);
    
    // Top face
    glNormal3f(0.0f, 1.0f, 0.0f);
    glVertex3f(-s, s, -s);
    glVertex3f(-s, s, s);
    glVertex3f(s, s, s);
    glVertex3f(s, s, -s);
    
    // Bottom face
    glNormal3f(0.0f, -1.0f, 0.0f);
    glVertex3f(-s, -s, -s);
    glVertex3f(s, -s, -s);
    glVertex3f(s, -s, s);
    glVertex3f(-s, -s, s);
    
    // Right face
    glNormal3f(1.0f, 0.0f, 0.0f);
    glVertex3f(s, -s, -s);
    glVertex3f(s, s, -s);
    glVertex3f(s, s, s);
    glVertex3f(s, -s, s);
    
    // Left face
    glNormal3f(-1.0f, 0.0f, 0.0f);
    glVertex3f(-s, -s, -s);
    glVertex3f(-s, -s, s);
    glVertex3f(-s, s, s);
    glVertex3f(-s, s, -s);
    glEnd();
    
    if (wireframe) {
        glPolygonMode(GL_FRONT_AND_BACK, GL_FILL);
    }
}

// Function to draw a sphere
void draw_sphere(float radius, float* color, int slices, int stacks) {
    glColor3fv(color);
    
    if (wireframe) {
        glPolygonMode(GL_FRONT_AND_BACK, GL_LINE);
        glLineWidth(2.0f);
    }
    
    GLUquadric* quadric = gluNewQuadric();
    if (wireframe) {
        gluQuadricDrawStyle(quadric, GLU_LINE);
    } else {
        gluQuadricDrawStyle(quadric, GLU_FILL);
    }
    
    gluSphere(quadric, radius, slices, stacks);
    gluDeleteQuadric(quadric);
    
    if (wireframe) {
        glPolygonMode(GL_FRONT_AND_BACK, GL_FILL);
    }
}

// Function to draw a pyramid
void draw_pyramid(float base, float height, float* color) {
    glColor3fv(color);
    
    if (wireframe) {
        glPolygonMode(GL_FRONT_AND_BACK, GL_LINE);
        glLineWidth(2.0f);
    }
    
    float b = base / 2.0f;
    
    glBegin(GL_TRIANGLES);
    // Front face
    glNormal3f(0.0f, 0.4472f, 0.8944f);
    glVertex3f(0.0f, height, 0.0f);
    glVertex3f(-b, 0.0f, b);
    glVertex3f(b, 0.0f, b);
    
    // Right face
    glNormal3f(0.8944f, 0.4472f, 0.0f);
    glVertex3f(0.0f, height, 0.0f);
    glVertex3f(b, 0.0f, b);
    glVertex3f(b, 0.0f, -b);
    
    // Back face
    glNormal3f(0.0f, 0.4472f, -0.8944f);
    glVertex3f(0.0f, height, 0.0f);
    glVertex3f(b, 0.0f, -b);
    glVertex3f(-b, 0.0f, -b);
    
    // Left face
    glNormal3f(-0.8944f, 0.4472f, 0.0f);
    glVertex3f(0.0f, height, 0.0f);
    glVertex3f(-b, 0.0f, -b);
    glVertex3f(-b, 0.0f, b);
    glEnd();
    
    // Base
    glBegin(GL_QUADS);
    glNormal3f(0.0f, -1.0f, 0.0f);
    glVertex3f(-b, 0.0f, b);
    glVertex3f(-b, 0.0f, -b);
    glVertex3f(b, 0.0f, -b);
    glVertex3f(b, 0.0f, b);
    glEnd();
    
    if (wireframe) {
        glPolygonMode(GL_FRONT_AND_BACK, GL_FILL);
    }
}

// Function to draw a cylinder
void draw_cylinder(float base, float top, float height, float* color, int slices) {
    glColor3fv(color);
    
    if (wireframe) {
        glPolygonMode(GL_FRONT_AND_BACK, GL_LINE);
        glLineWidth(2.0f);
    }
    
    GLUquadric* quadric = gluNewQuadric();
    if (wireframe) {
        gluQuadricDrawStyle(quadric, GLU_LINE);
    } else {
        gluQuadricDrawStyle(quadric, GLU_FILL);
    }
    
    gluCylinder(quadric, base, top, height, slices, 1);
    gluDeleteQuadric(quadric);
    
    if (wireframe) {
        glPolygonMode(GL_FRONT_AND_BACK, GL_FILL);
    }
}

// Function to draw coordinate axes
void draw_axes(float length) {
    glDisable(GL_LIGHTING);
    
    glLineWidth(3.0f);
    glBegin(GL_LINES);
    
    // X axis (Red)
    glColor3f(1.0f, 0.0f, 0.0f);
    glVertex3f(0.0f, 0.0f, 0.0f);
    glVertex3f(length, 0.0f, 0.0f);
    
    // Y axis (Green)
    glColor3f(0.0f, 1.0f, 0.0f);
    glVertex3f(0.0f, 0.0f, 0.0f);
    glVertex3f(0.0f, length, 0.0f);
    
    // Z axis (Blue)
    glColor3f(0.0f, 0.0f, 1.0f);
    glVertex3f(0.0f, 0.0f, 0.0f);
    glVertex3f(0.0f, 0.0f, length);
    
    glEnd();
    
    // Draw axis labels
    glRasterPos3f(length + 0.2f, 0.0f, 0.0f);
    glutBitmapCharacter(GLUT_BITMAP_HELVETICA_18, 'X');
    
    glRasterPos3f(0.0f, length + 0.2f, 0.0f);
    glutBitmapCharacter(GLUT_BITMAP_HELVETICA_18, 'Y');
    
    glRasterPos3f(0.0f, 0.0f, length + 0.2f);
    glutBitmapCharacter(GLUT_BITMAP_HELVETICA_18, 'Z');
    
    if (lighting_enabled) {
        glEnable(GL_LIGHTING);
    }
}

// Function to draw a grid
void draw_grid(int size, int step) {
    glDisable(GL_LIGHTING);
    
    glColor3f(0.5f, 0.5f, 0.5f);
    glLineWidth(1.0f);
    
    glBegin(GL_LINES);
    for (int i = -size; i <= size; i += step) {
        // Lines along X direction
        glVertex3f(i, 0.0f, -size);
        glVertex3f(i, 0.0f, size);
        
        // Lines along Z direction
        glVertex3f(-size, 0.0f, i);
        glVertex3f(size, 0.0f, i);
    }
    glEnd();
    
    if (lighting_enabled) {
        glEnable(GL_LIGHTING);
    }
}

// Function to setup perspective projection
void setup_perspective() {
    glMatrixMode(GL_PROJECTION);
    glLoadIdentity();
    
    float aspect = (float)window_width / window_height;
    gluPerspective(camera.fov, aspect, 0.1f, 100.0f);
    
    glMatrixMode(GL_MODELVIEW);
    glLoadIdentity();
}

// Function to set camera view
void set_camera_view() {
    glRotatef(camera.rx, 1.0f, 0.0f, 0.0f);
    glRotatef(camera.ry, 0.0f, 1.0f, 0.0f);
    glTranslatef(-camera.x, -camera.y, -camera.z);
}

// Display callback function
void display() {
    glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
    
    setup_perspective();
    set_camera_view();
    
    // Draw grid if enabled
    if (show_grid) {
        draw_grid(10, 1);
    }
    
    // Draw axes if enabled
    if (show_axes) {
        draw_axes(2.0f);
    }
    
    // Draw all objects
    for (int i = 0; i < num_objects; i++) {
        glPushMatrix();
        
        // Apply transformations
        glTranslatef(objects[i].x, objects[i].y, objects[i].z);
        glRotatef(objects[i].rx + rotation_angle, 1.0f, 0.0f, 0.0f);
        glRotatef(objects[i].ry + rotation_angle, 0.0f, 1.0f, 0.0f);
        glRotatef(objects[i].rz + rotation_angle, 0.0f, 0.0f, 1.0f);
        glScalef(objects[i].scale, objects[i].scale, objects[i].scale);
        
        // Draw based on object type
        switch (objects[i].type) {
            case 0:  // Cube
                draw_cube(1.0f, objects[i].color);
                break;
            case 1:  // Sphere
                draw_sphere(0.5f, objects[i].color, 16, 16);
                break;
            case 2:  // Pyramid
                draw_pyramid(1.0f, 1.0f, objects[i].color);
                break;
            case 3:  // Cylinder
                draw_cylinder(0.3f, 0.3f, 1.0f, objects[i].color, 16);
                break;
        }
        
        glPopMatrix();
    }
    
    // Draw rotation angle text
    glDisable(GL_LIGHTING);
    glMatrixMode(GL_PROJECTION);
    glPushMatrix();
    glLoadIdentity();
    gluOrtho2D(0, window_width, 0, window_height);
    
    glMatrixMode(GL_MODELVIEW);
    glPushMatrix();
    glLoadIdentity();
    
    glColor3f(1.0f, 1.0f, 1.0f);
    glRasterPos2f(10, window_height - 20);
    
    char info[256];
    snprintf(info, 256, "Rotation: %.1f degrees | Objects: %d | Camera: (%.1f, %.1f, %.1f)", 
             rotation_angle, num_objects, camera.x, camera.y, camera.z);
    
    for (int i = 0; info[i] != '\0'; i++) {
        glutBitmapCharacter(GLUT_BITMAP_HELVETICA_12, info[i]);
    }
    
    glPopMatrix();
    glMatrixMode(GL_PROJECTION);
    glPopMatrix();
    glMatrixMode(GL_MODELVIEW);
    
    if (lighting_enabled) {
        glEnable(GL_LIGHTING);
    }
    
    glutSwapBuffers();
}

// Reshape callback function
void reshape(int width, int height) {
    window_width = width;
    window_height = height;
    
    glViewport(0, 0, width, height);
    glutPostRedisplay();
}

// Timer callback for animation
void timer(int value) {
    rotation_angle += 1.0f;
    if (rotation_angle > 360.0f) {
        rotation_angle -= 360.0f;
    }
    
    // Move camera in a circular path
    static float camera_angle = 0.0f;
    camera_angle += 0.5f;
    camera.x = sin(camera_angle * 3.14159f / 180.0f) * 10.0f;
    camera.z = cos(camera_angle * 3.14159f / 180.0f) * 10.0f;
    
    glutPostRedisplay();
    glutTimerFunc(16, timer, 0);  // ~60 FPS
}

// Keyboard callback function
void keyboard(unsigned char key, int x, int y) {
    switch (key) {
        case 27:  // ESC key
            exit(0);
            break;
        case ' ':
            create_random_objects();
            break;
        case 'a':
            show_axes = !show_axes;
            break;
        case 'g':
            show_grid = !show_grid;
            break;
        case 'w':
            wireframe = !wireframe;
            break;
        case 'l':
            lighting_enabled = !lighting_enabled;
            if (lighting_enabled) {
                glEnable(GL_LIGHTING);
            } else {
                glDisable(GL_LIGHTING);
            }
            break;
        case '+':
            num_objects = (num_objects < 100) ? num_objects + 5 : num_objects;
            create_random_objects();
            break;
        case '-':
            num_objects = (num_objects > 5) ? num_objects - 5 : num_objects;
            create_random_objects();
            break;
        case 'c':
            camera.x = 0;
            camera.y = 0;
            camera.z = 5;
            camera.rx = 0;
            camera.ry = 0;
            break;
    }
    glutPostRedisplay();
}

// Special keyboard callback for arrow keys
void special_keys(int key, int x, int y) {
    switch (key) {
        case GLUT_KEY_UP:
            camera.rx -= 5.0f;
            break;
        case GLUT_KEY_DOWN:
            camera.rx += 5.0f;
            break;
        case GLUT_KEY_LEFT:
            camera.ry -= 5.0f;
            break;
        case GLUT_KEY_RIGHT:
            camera.ry += 5.0f;
            break;
        case GLUT_KEY_PAGE_UP:
            camera.z -= 0.5f;
            break;
        case GLUT_KEY_PAGE_DOWN:
            camera.z += 0.5f;
            break;
        case GLUT_KEY_HOME:
            camera.fov = (camera.fov < 90.0f) ? camera.fov + 5.0f : 90.0f;
            break;
        case GLUT_KEY_END:
            camera.fov = (camera.fov > 15.0f) ? camera.fov - 5.0f : 15.0f;
            break;
    }
    glutPostRedisplay();
}

// Mouse callback for object selection (simplified)
void mouse(int button, int state, int x, int y) {
    if (button == GLUT_LEFT_BUTTON && state == GLUT_DOWN) {
        printf("Mouse click at: %d, %d\n", x, y);
        
        // Simple object selection (just change color of first object)
        if (num_objects > 0) {
            objects[0].color[0] = (rand() % 100) / 100.0f;
            objects[0].color[1] = (rand() % 100) / 100.0f;
            objects[0].color[2] = (rand() % 100) / 100.0f;
            glutPostRedisplay();
        }
    }
}

// Menu callback function
void menu(int option) {
    switch (option) {
        case 1:
            show_axes = !show_axes;
            break;
        case 2:
            show_grid = !show_grid;
            break;
        case 3:
            wireframe = !wireframe;
            break;
        case 4:
            lighting_enabled = !lighting_enabled;
            if (lighting_enabled) {
                glEnable(GL_LIGHTING);
            } else {
                glDisable(GL_LIGHTING);
            }
            break;
        case 5:
            create_random_objects();
            break;
        case 6:
            exit(0);
            break;
    }
    glutPostRedisplay();
}

// Create popup menu
void create_menu() {
    int submenu = glutCreateMenu(menu);
    glutAddMenuEntry("Toggle Axes", 1);
    glutAddMenuEntry("Toggle Grid", 2);
    glutAddMenuEntry("Toggle Wireframe", 3);
    glutAddMenuEntry("Toggle Lighting", 4);
    glutAddMenuEntry("Randomize Objects", 5);
    glutAddMenuEntry("Exit", 6);
    
    glutAttachMenu(GLUT_RIGHT_BUTTON);
}

// Main function
int main(int argc, char** argv) {
    // Initialize GLUT
    glutInit(&argc, argv);
    glutInitDisplayMode(GLUT_DOUBLE | GLUT_RGB | GLUT_DEPTH);
    glutInitWindowSize(window_width, window_height);
    glutInitWindowPosition(100, 100);
    glutCreateWindow("OpenGL 3D Scene in Pure C");
    
    // Initialize OpenGL
    init_opengl();
    
    // Create random objects
    create_random_objects();
    
    // Set callback functions
    glutDisplayFunc(display);
    glutReshapeFunc(reshape);
    glutKeyboardFunc(keyboard);
    glutSpecialFunc(special_keys);
    glutMouseFunc(mouse);
    glutTimerFunc(0, timer, 0);
    
    // Create menu
    create_menu();
    
    // Print instructions
    printf("OpenGL 3D Scene Demo\n");
    printf("====================\n");
    printf("Controls:\n");
    printf("  ESC: Exit\n");
    printf("  Space: Randomize objects\n");
    printf("  A: Toggle axes\n");
    printf("  G: Toggle grid\n");
    printf("  W: Toggle wireframe\n");
    printf("  L: Toggle lighting\n");
    printf("  +: Add objects\n");
    printf("  -: Remove objects\n");
    printf("  C: Reset camera\n");
    printf("  Arrow keys: Rotate camera\n");
    printf("  Page Up/Down: Zoom in/out\n");
    printf("  Home/End: Change FOV\n");
    printf("  Right-click: Menu\n");
    
    // Start main loop
    glutMainLoop();
    
    // Cleanup
    if (objects) {
        free(objects);
    }
    
    return 0;
}
