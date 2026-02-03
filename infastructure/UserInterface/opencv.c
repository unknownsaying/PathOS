#include <opencv2/core/core_c.h>
#include <opencv2/highgui/highgui_c.h>
#include <opencv2/imgproc/imgproc_c.h>
#include <stdio.h>
#include <stdlib.h>
#include <time.h>

// Structure for image data
typedef struct {
    IplImage* image;
    int width;
    int height;
    int channels;
} ImageData;

// Structure for detected object
typedef struct {
    CvRect rect;
    CvPoint center;
    double area;
    char label[50];
} DetectedObject;

// Function to create image data structure
ImageData* create_image_data(const char* filename) {
    ImageData* img_data = (ImageData*)malloc(sizeof(ImageData));
    if (!img_data) {
        fprintf(stderr, "Memory allocation failed\n");
        return NULL;
    }
    
    // Load image
    img_data->image = cvLoadImage(filename, CV_LOAD_IMAGE_COLOR);
    if (!img_data->image) {
        fprintf(stderr, "Could not load image: %s\n", filename);
        free(img_data);
        return NULL;
    }
    
    img_data->width = img_data->image->width;
    img_data->height = img_data->image->height;
    img_data->channels = img_data->image->nChannels;
    
    return img_data;
}

// Function to release image data
void release_image_data(ImageData* img_data) {
    if (img_data) {
        if (img_data->image) {
            cvReleaseImage(&img_data->image);
        }
        free(img_data);
    }
}

// Function to convert to grayscale
IplImage* convert_to_grayscale(const IplImage* color_img) {
    IplImage* gray_img = cvCreateImage(
        cvGetSize(color_img),
        IPL_DEPTH_8U,
        1
    );
    
    cvCvtColor(color_img, gray_img, CV_BGR2GRAY);
    return gray_img;
}

// Function to apply edge detection (Canny)
IplImage* detect_edges(const IplImage* gray_img) {
    IplImage* edges = cvCreateImage(
        cvGetSize(gray_img),
        IPL_DEPTH_8U,
        1
    );
    
    // Apply Gaussian blur first
    IplImage* blurred = cvCreateImage(cvGetSize(gray_img), IPL_DEPTH_8U, 1);
    cvSmooth(gray_img, blurred, CV_GAUSSIAN, 5, 5, 0, 0);
    
    // Apply Canny edge detection
    cvCanny(blurred, edges, 50, 150, 3);
    
    cvReleaseImage(&blurred);
    return edges;
}

// Function to detect objects (contours)
DetectedObject* detect_objects(const IplImage* edges, int* num_objects) {
    CvMemStorage* storage = cvCreateMemStorage(0);
    CvSeq* contours = NULL;
    
    // Find contours
    cvFindContours(
        edges,
        storage,
        &contours,
        sizeof(CvContour),
        CV_RETR_EXTERNAL,
        CV_CHAIN_APPROX_SIMPLE,
        cvPoint(0, 0)
    );
    
    // Count contours
    int count = 0;
    CvSeq* ptr = contours;
    while (ptr) {
        count++;
        ptr = ptr->h_next;
    }
    
    // Allocate array for detected objects
    DetectedObject* objects = (DetectedObject*)malloc(count * sizeof(DetectedObject));
    if (!objects) {
        *num_objects = 0;
        cvReleaseMemStorage(&storage);
        return NULL;
    }
    
    // Extract object information
    ptr = contours;
    int i = 0;
    while (ptr) {
        if (ptr->total > 0) {
            // Get bounding rectangle
            objects[i].rect = cvBoundingRect(ptr, 0);
            
            // Calculate center
            objects[i].center.x = objects[i].rect.x + objects[i].rect.width / 2;
            objects[i].center.y = objects[i].rect.y + objects[i].rect.height / 2;
            
            // Calculate area
            objects[i].area = fabs(cvContourArea(ptr));
            
            // Generate label based on shape approximation
            CvSeq* approx = cvApproxPoly(
                ptr,
                sizeof(CvContour),
                storage,
                CV_POLY_APPROX_DP,
                cvContourPerimeter(ptr) * 0.02,
                0
            );
            
            switch (approx->total) {
                case 3:
                    snprintf(objects[i].label, 50, "Triangle");
                    break;
                case 4: {
                    CvRect rect = cvBoundingRect(ptr, 0);
                    float aspect = (float)rect.width / rect.height;
                    if (aspect > 0.95 && aspect < 1.05) {
                        snprintf(objects[i].label, 50, "Square");
                    } else {
                        snprintf(objects[i].label, 50, "Rectangle");
                    }
                    break;
                }
                default: {
                    // Check if it's a circle
                    double area = cvContourArea(ptr);
                    double perimeter = cvArcLength(ptr, CV_WHOLE_SEQ, 1);
                    double circularity = 4 * CV_PI * area / (perimeter * perimeter);
                    
                    if (circularity > 0.7) {
                        snprintf(objects[i].label, 50, "Circle");
                    } else {
                        snprintf(objects[i].label, 50, "Polygon");
                    }
                    break;
                }
            }
            
            i++;
        }
        ptr = ptr->h_next;
    }
    
    *num_objects = i;
    cvReleaseMemStorage(&storage);
    
    // Resize array if some contours were skipped
    if (i < count) {
        DetectedObject* resized = (DetectedObject*)realloc(objects, i * sizeof(DetectedObject));
        if (resized) {
            objects = resized;
        }
    }
    
    return objects;
}

// Function to draw detected objects on image
void draw_objects(IplImage* image, DetectedObject* objects, int num_objects) {
    CvFont font;
    cvInitFont(&font, CV_FONT_HERSHEY_SIMPLEX, 0.5, 0.5, 0, 1, CV_AA);
    
    for (int i = 0; i < num_objects; i++) {
        // Draw rectangle
        cvRectangle(
            image,
            cvPoint(objects[i].rect.x, objects[i].rect.y),
            cvPoint(objects[i].rect.x + objects[i].rect.width, 
                   objects[i].rect.y + objects[i].rect.height),
            CV_RGB(0, 255, 0),
            2,
            CV_AA,
            0
        );
        
        // Draw center point
        cvCircle(
            image,
            objects[i].center,
            3,
            CV_RGB(255, 0, 0),
            -1,
            CV_AA,
            0
        );
        
        // Draw label
        char text[100];
        snprintf(text, 100, "%s (%.0f)", objects[i].label, objects[i].area);
        
        cvPutText(
            image,
            text,
            cvPoint(objects[i].rect.x, objects[i].rect.y - 5),
            &font,
            CV_RGB(255, 255, 0)
        );
    }
}

// Function to process video from camera
void process_camera_feed() {
    CvCapture* capture = cvCreateCameraCapture(0);
    if (!capture) {
        fprintf(stderr, "Cannot open camera\n");
        return;
    }
    
    // Create windows
    cvNamedWindow("Original", CV_WINDOW_AUTOSIZE);
    cvNamedWindow("Edges", CV_WINDOW_AUTOSIZE);
    cvNamedWindow("Objects", CV_WINDOW_AUTOSIZE);
    
    IplImage* frame = NULL;
    IplImage* gray = NULL;
    IplImage* edges = NULL;
    IplImage* result = NULL;
    
    printf("Press ESC to exit camera feed\n");
    
    while (1) {
        frame = cvQueryFrame(capture);
        if (!frame) break;
        
        // Convert to grayscale
        if (gray) cvReleaseImage(&gray);
        gray = convert_to_grayscale(frame);
        
        // Detect edges
        if (edges) cvReleaseImage(&edges);
        edges = detect_edges(gray);
        
        // Detect objects
        int num_objects = 0;
        DetectedObject* objects = detect_objects(edges, &num_objects);
        
        // Create result image
        if (result) cvReleaseImage(&result);
        result = cvCloneImage(frame);
        
        // Draw objects on result
        if (objects && num_objects > 0) {
            draw_objects(result, objects, num_objects);
            free(objects);
        }
        
        // Display images
        cvShowImage("Original", frame);
        cvShowImage("Edges", edges);
        cvShowImage("Objects", result);
        
        // Check for ESC key
        if (cvWaitKey(10) == 27) break;
    }
    
    // Cleanup
    cvDestroyAllWindows();
    cvReleaseCapture(&capture);
    if (gray) cvReleaseImage(&gray);
    if (edges) cvReleaseImage(&edges);
    if (result) cvReleaseImage(&result);
}

// Function to process a single image
void process_image(const char* filename) {
    printf("Processing image: %s\n", filename);
    
    // Load image
    ImageData* img_data = create_image_data(filename);
    if (!img_data) return;
    
    printf("Image loaded: %dx%d, %d channels\n", 
           img_data->width, img_data->height, img_data->channels);
    
    // Convert to grayscale
    IplImage* gray_img = convert_to_grayscale(img_data->image);
    
    // Detect edges
    IplImage* edges = detect_edges(gray_img);
    
    // Detect objects
    int num_objects = 0;
    DetectedObject* objects = detect_objects(edges, &num_objects);
    
    printf("Detected %d objects\n", num_objects);
    
    // Create result image
    IplImage* result = cvCloneImage(img_data->image);
    draw_objects(result, objects, num_objects);
    
    // Display images
    cvNamedWindow("Original Image", CV_WINDOW_AUTOSIZE);
    cvNamedWindow("Grayscale", CV_WINDOW_AUTOSIZE);
    cvNamedWindow("Edges", CV_WINDOW_AUTOSIZE);
    cvNamedWindow("Detected Objects", CV_WINDOW_AUTOSIZE);
    
    cvShowImage("Original Image", img_data->image);
    cvShowImage("Grayscale", gray_img);
    cvShowImage("Edges", edges);
    cvShowImage("Detected Objects", result);
    
    printf("Press any key to continue...\n");
    cvWaitKey(0);
    
    // Cleanup
    cvDestroyAllWindows();
    free(objects);
    cvReleaseImage(&gray_img);
    cvReleaseImage(&edges);
    cvReleaseImage(&result);
    release_image_data(img_data);
}

// Main function
int main(int argc, char** argv) {
    // Initialize OpenCV
    printf("OpenCV C Demo\n");
    printf("=============\n");
    
    if (argc > 1) {
        // Process image file
        process_image(argv[1]);
    } else {
        printf("No image file provided. Using camera feed.\n");
        process_camera_feed();
    }
    
    printf("Program completed successfully.\n");
    return 0;
}
