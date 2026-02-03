#include <opencv2/core/core_c.h>
#include <opencv2/highgui/highgui_c.h>
#include <GL/gl.h>
#include <GL/glu.h>
#include <GL/glut.h>
#include <stdio.h>
#include <stdlib.h>

// Structure to hold OpenCV texture data
typedef struct {
    unsigned char* data;
    int width;
    int height;
    int channels;
    GLuint texture_id;
} CVTexture;

// Global variables
CVTexture cv_texture;
int window_width = 800;
int window_height = 600;
float rotation_angle = 0.0f;
CvCapture* camera = NULL;

// Function to load image from file
int load_image(const char* filename) {
    IplImage* img = cvLoadImage(filename, CV_LOAD_IMAGE_COLOR);
    if (!img) {
        fprintf(stderr, "Cannot load image: %s\n", filename);
        return 0;
    }
    
    cv_texture.width = img->width;
    cv_texture.height = img->height;
    cv_texture.channels = img->nChannels;
    
    // Allocate memory
    cv_texture.data = (unsigned char*)malloc(
        cv_texture.width * cv_texture.height * cv_texture.channels
    );
    
    if (!cv_texture.data) {
        fprintf(stderr, "Memory allocation failed\n");
        cvReleaseImage(&img);
        return 0;
    }
    
    // Copy data
    memcpy(cv_texture.data, img->imageData,
           cv_texture.width * cv_texture.height * cv_texture.channels);
    
    cvReleaseImage(&img);
    return 1;
}

// Function to initialize camera
int init_camera(int device_id) {
    camera = cvCreateCameraCapture(device_id);
    if (!camera) {
        fprintf(stderr, "Cannot open camera\n");
        return 0;
    }
    
    // Get first frame to determine size
    IplImage* frame = cvQueryFrame(camera);
    if (!frame) {
        fprintf(stderr, "Cannot get frame from camera\n");
        cvReleaseCapture(&camera);
        return 0;
    }
    
    cv_texture.width = frame->width;
    cv_texture.height = frame->height;
    cv_texture.channels = frame->nChannels;
    
    // Allocate memory
    cv_texture.data = (unsigned char*)malloc(
        cv_texture.width * cv_texture.height * cv_texture.channels
    );
    
    if (!cv_texture.data) {
        fprintf(stderr, "Memory allocation failed\n");
        cvReleaseCapture(&camera);
        return 0;
    }
    
    return 1;
}

// Function to update texture from camera
void update_from_camera() {
    if (!camera) return;
    
    IplImage* frame = cvQueryFrame(camera);
    if (frame) {
        // Convert BGR to RGB for OpenGL
        IplImage* rgb_frame = cvCreateImage(cvGetSize(frame), IPL_DEPTH_8U, 3);
        cvCvtColor(frame, rgb_frame, CV_BGR2RGB);
        
        memcpy(cv_texture.data, rgb_frame->imageData,
               cv_texture.width * cv_texture.height * cv_texture.channels);
        
        cvReleaseImage(&rgb_frame);
        
        // Update OpenGL texture
        glBindTexture(GL_TEXTURE_2D, cv_texture.texture_id);
        glTexImage2D(GL_TEXTURE_2D, 0, GL_RGB,
                     cv_texture.width, cv_texture.height,
                     0, GL_RGB, GL_UNSIGNED_BYTE,
                     cv_texture.data);
    }
}

// Function to create OpenGL texture
void create_opengl_texture() {
    glGenTextures(1, &cv_texture.texture_id);
    glBindTexture(GL_TEXTURE_2D, cv_texture.texture_id);
    
    // Set texture parameters
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP);
    
    // Load texture data
    glTexImage2D(GL_TEXTURE_2D, 0, GL_RGB,
                 cv_texture.width, cv_texture.height,
                 0, GL_RGB, GL_UNSIGNED_BYTE,
                 cv_texture.data);
}

// Function to process image with OpenCV (edge detection)
void process_image_with_opencv() {
    if (!cv_texture.data) return;
    
    // Create OpenCV image from texture data
    IplImage* cv_img = cvCreateImageHeader(
        cvSize(cv_texture.width, cv_texture.height),
        IPL_DEPTH_8U,
        cv_texture.channels
    );
    
    cvSetData(cv_img, cv_texture.data,
              cv_texture.width * cv_texture.channels);
    
    // Convert to grayscale
    IplImage* gray = cvCreateImage(cvGetSize(cv_img), IPL_DEPTH_8U, 1);
    cvCvtColor(cv_img, gray, CV_RGB2GRAY);
    
    // Apply edge detection
    IplImage* edges = cvCreateImage(cvGetSize(cv_img), IPL_DEPTH_8U, 1);
    cvCanny(gray, edges, 50, 150, 3);
    
    // Convert edges to RGB
    IplImage* edges_rgb = cvCreateImage(cvGetSize(cv_img), IPL_DEPTH_8U, 3);
    cvCvtColor(edges, edges_rgb, CV_GRAY2RGB);
    
    // Copy processed data back to texture
    memcpy(cv_texture.data, edges_rgb->imageData,
           cv_texture.width * cv_texture.height * cv_texture.channels);
    
    // Cleanup
    cvReleaseImageHeader(&cv_img);
    cvReleaseImage(&gray);
    cvReleaseImage(&edges);
    cvReleaseImage(&edges_rgb);
    
    // Update OpenGL texture
    glBindTexture(GL_TEXTURE_2D, cv_texture.texture_id);
    glTexImage2D(GL_TEXTURE_2D, 0, GL_RGB,
                 cv_texture.width, cv_texture.height,
                 0, GL_RGB, GL_UNSIGNED_BYTE,
                 cv_texture.data);
}

// OpenGL initialization
void init_opengl() {
    glClearColor(0.0f, 0.0f, 0.0f, 1.0f);
    glEnable(GL_DEPTH_TEST);
    glEnable(GL_TEXTURE_2D);
    
    // Set up lighting
    glEnable(GL_LIGHTING);
    glEnable(GL_LIGHT0);
    
    GLfloat light_pos[] = {5.0f, 5.0f, 5.0f, 1.0f};
    GLfloat light_ambient[] = {0.2f, 0.2f, 0.2f, 1.0f};
    GLfloat light_diffuse[] = {0.8f, 0.8f, 0.8f, 1.0f};
    
    glLightfv(GL_LIGHT0, GL_POSITION, light_pos);
    glLightfv(GL_LIGHT0, GL_AMBIENT, light_ambient);
    glLightfv(GL_LIGHT0, GL_DIFFUSE, light_diffuse);
}

// Display callback
void display() {
    glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
    
    // Update from camera if available
    if (camera) {
        update_from_camera();
    }
    
    // Set up projection
    glMatrixMode(GL_PROJECTION);
    glLoadIdentity();
    gluPerspective(45.0f, (float)window_width/window_height, 0.1f, 100.0f);
    
    // Set up modelview
    glMatrixMode(GL_MODELVIEW);
    glLoadIdentity();
    
    // Set camera position
    gluLookAt(0, 0, 5,    // eye position
              0, 0, 0,    // look at position
              0, 1, 0);   // up vector
    
    // Rotate scene
    glRotatef(rotation_angle, 0.0f, 1.0f, 0.0f);
    
    // Draw textured cube
    glBindTexture(GL_TEXTURE_2D, cv_texture.texture_id);
    
    glBegin(GL_QUADS);
    // Front face
    glNormal3f(0.0f, 0.0f, 1.0f);
    glTexCoord2f(0.0f, 0.0f); glVertex3f(-1.0f, -1.0f, 1.0f);
    glTexCoord2f(1.0f, 0.0f); glVertex3f(1.0f, -1.0f, 1.0f);
    glTexCoord2f(1.0f, 1.0f); glVertex3f(1.0f, 1.0f, 1.0f);
    glTexCoord2f(0.0f, 1.0f); glVertex3f(-1.0f, 1.0f, 1.0f);
    
    // Back face
    glNormal3f(0.0f, 0.0f, -1.0f);
    glTexCoord2f(1.0f, 0.0f); glVertex3f(-1.0f, -1.0f, -1.0f);
    glTexCoord2f(1.0f, 1.0f); glVertex3f(-1.0f, 1.0f, -1.0f);
    glTexCoord2f(0.0f, 1.0f); glVertex3f(1.0f, 1.0f, -1.0f);
    glTexCoord2f(0.0f, 0.0f); glVertex3f(1.0f, -1.0f, -1.0f);
    
    // Top face
    glNormal3f(0.0f, 1.0f, 0.0f);
    glTexCoord2f(0.0f, 1.0f); glVertex3f(-1.0f, 1.0f, -1.0f);
    glTexCoord2f(0.0f, 0.0f); glVertex3f(-1.0f, 1.0f, 1.0f);
    glTexCoord2f(1.0f, 0.0f); glVertex3f(1.0f, 1.0f, 1.0f);
    glTexCoord2f(1.0f, 1.0f); glVertex3f(1.0f, 1.0f, -1.0f);
    
    // Bottom face
    glNormal3f(0.0f, -1.0f, 0.0f);
    glTexCoord2f(1.0f, 1.0f); glVertex3f(-1.0f, -1.0f, -1.0f);
    glTexCoord2f(0.0f, 1.0f); glVertex3f(1.0f, -1.0f, -1.0f);
    glTexCoord2f(0.0f, 0.0f); glVertex3f(1.0f, -1.0f, 1.0f);
    glTexCoord2f(1.0f, 0.0f); glVertex3f(-1.0f, -1.0f, 1.0f);
    
    // Right face
    glNormal3f(1.0f, 0.0f, 0.0f);
    glTexCoord2f(1.0f, 0.0f); glVertex3f(1.0f, -1.0f, -1.0f);
    glTexCoord2f(1.0f, 1.0f); glVertex3f(1.0f, 1.0f, -1.0f);
    glTexCoord2f(0.0f, 1.0f); glVertex3f(1.0f, 1.0f, 1.0f);
    glTexCoord2f(0.0f, 0.0f); glVertex3f(1.0f, -1.0f, 1.0f);
    
    // Left face
    glNormal3f(-1.0f, 0.0f, 0.0f);
    glTexCoord2f(0.0f, 0.0f); glVertex3f(-1.0f, -1.0f, -1.0f);
    glTexCoord2f(1.0f, 0.0f); glVertex3f(-1.0f, -1.0f, 1.0f);
    glTexCoord2f(1.0f, 1.0f); glVertex3f(-1.0f, 1.0f, 1.0f);
    glTexCoord2f(0.0f, 1.0f); glVertex3f(-1.0f, 1.0f, -1.0f);
    glEnd();
    
    glutSwapBuffers();
}

// Reshape callback
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
    
    glutPostRedisplay();
    glutTimerFunc(16, timer, 0);  // ~60 FPS
}

// Keyboard callback
void keyboard(unsigned char key, int x, int y) {
    switch (key) {
        case 27:  // ESC key
            exit(0);
            break;
        case 'p':
        case 'P':
            process_image_with_opencv();
            break;
        case 'r':
        case 'R':
            // Reload texture
            if (camera) {
                update_from_camera();
            }
            break;
    }
    glutPostRedisplay();
}

// Cleanup function
void cleanup() {
    if (cv_texture.data) {
        free(cv_texture.data);
        cv_texture.data = NULL;
    }
    
    if (camera) {
        cvReleaseCapture(&camera);
    }
    
    if (cv_texture.texture_id) {
        glDeleteTextures(1, &cv_texture.texture_id);
    }
}

// Main function
int main(int argc, char** argv) {
    // Initialize GLUT
    glutInit(&argc, argv);
    glutInitDisplayMode(GLUT_DOUBLE | GLUT_RGB | GLUT_DEPTH);
    glutInitWindowSize(window_width, window_height);
    glutCreateWindow("OpenCV + OpenGL Integration");
    
    // Initialize OpenGL
    init_opengl();
    
    // Load image or initialize camera
    if (argc > 1) {
        // Load from file
        if (!load_image(argv[1])) {
            fprintf(stderr, "Using default camera\n");
            if (!init_camera(0)) {
                fprintf(stderr, "Failed to initialize camera\n");
                return 1;
            }
        }
    } else {
        // Use camera
        if (!init_camera(0)) {
            fprintf(stderr, "Failed to initialize camera\n");
            return 1;
        }
    }
    
    // Create OpenGL texture
    create_opengl_texture();
    
    // Set callback functions
    glutDisplayFunc(display);
    glutReshapeFunc(reshape);
    glutKeyboardFunc(keyboard);
    glutTimerFunc(0, timer, 0);
    
    // Print instructions
    printf("OpenCV + OpenGL Demo\n");
    printf("====================\n");
    printf("Controls:\n");
    printf("  ESC: Exit\n");
    printf("  P: Process image (edge detection)\n");
    printf("  R: Reset/Reload texture\n");
    
    // Set cleanup function
    atexit(cleanup);
    
    // Start main loop
    glutMainLoop();
    
    return 0;
}
