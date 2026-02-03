#include <stdio.h>
#include <stdlib.h>
#include <math.h>
#include <complex.h>
#include <time.h>

// ============================================================================
// STEPHEN HAWKING'S PHYSICAL CONSTANTS (CODATA 2018 values)
// ============================================================================

// Fundamental constants used in Hawking's equations
#define PI 3.14159265358979323846
#define PI_SQUARED (PI * PI)
#define FOUR_PI (4.0 * PI)
#define EIGHT_PI (8.0 * PI)
#define SIXTEEN_PI (16.0 * PI)

// Speed of light in vacuum (m/s)
#define C 299792458.0
#define C_SQUARED (C * C)
#define C_CUBED (C * C * C)
#define C_FOURTH (C * C * C * C)

// Gravitational constant (m³/kg·s²)
#define G 6.67430e-11
#define G_SQUARED (G * G)

// Planck constant (J·s)
#define H 6.62607015e-34
#define HBAR (H / (2.0 * PI))
#define HBAR_SQUARED (HBAR * HBAR)

// Boltzmann constant (J/K)
#define K_B 1.380649e-23

// Stefan-Boltzmann constant (W/m²·K⁴)
#define SIGMA (2.0 * PI * PI * PI * PI * PI * K_B * K_B * K_B * K_B / 
               (15.0 * H * H * H * C * C))

// Planck units (derived from fundamental constants)
#define L_PLANCK sqrt(HBAR * G / C_CUBED)                     // Planck length
#define M_PLANCK sqrt(HBAR * C / G)                          // Planck mass
#define T_PLANCK sqrt(HBAR * G / C_FIFTH)                    // Planck time
#define T_PLANCK_K sqrt(HBAR * C_CUBED / (G * K_B * K_B))    // Planck temperature

// Electromagnetic constants (for charged black holes)
#define EPSILON_0 8.8541878128e-12      // Vacuum permittivity
#define MU_0 1.25663706212e-6           // Vacuum permeability
#define ELEMENTARY_CHARGE 1.602176634e-19

// Cosmological constant (Λ) - current estimate
#define COSMOLOGICAL_LAMBDA 1.1056e-52   // m⁻² (from Planck 2018)

// ============================================================================
// HAWKING'S KEY EQUATIONS - STRUCTURES AND FUNCTIONS
// ============================================================================

// Structure for black hole parameters
typedef struct {
    double mass;                // Mass in kg
    double charge;              // Electric charge in Coulombs
    double angular_momentum;    // Angular momentum in kg·m²/s (J·s)
    double schwarzschild_radius; // Schwarzschild radius in meters
    double area;                // Event horizon area in m²
    double entropy;             // Bekenstein-Hawking entropy
    double temperature;         // Hawking temperature in Kelvin
    double luminosity;          // Hawking radiation power in Watts
    double lifetime;            // Evaporation time in seconds
    double specific_heat;       // Black hole specific heat capacity
    double surface_gravity;     // Surface gravity (κ) in m/s²
    double omega;               // Angular velocity (for Kerr black holes)
} BlackHole;

// Structure for Hawking radiation spectrum
typedef struct {
    double frequency;           // Frequency in Hz
    double spectral_density;    // Energy per frequency interval
    double particle_flux;       // Number of particles emitted per second
    double energy_flux;         // Energy emitted per second
} RadiationSpectrum;

// Structure for information paradox analysis
typedef struct {
    double initial_entropy;     // Initial black hole entropy
    double radiation_entropy;   // Entropy of emitted radiation
    double information_loss;    // Information loss (if any)
    double purity;              // Purity of final state (0 to 1)
    double page_time;           // Page time (when entropy starts decreasing)
} InformationParadox;

// ============================================================================
// SCHWARZSCHILD BLACK HOLE (NON-ROTATING, UNCHARGED)
// ============================================================================

// Hawking's Equation 1: Schwarzschild radius
// R_s = 2GM/c²
double schwarzschild_radius(double mass) {
    return 2.0 * G * mass / C_SQUARED;
}

// Hawking's Equation 2: Event horizon area
// A = 4πR_s² = 16πG²M²/c⁴
double horizon_area(double mass) {
    double rs = schwarzschild_radius(mass);
    return FOUR_PI * rs * rs;
}

// Hawking's Equation 3: Bekenstein-Hawking entropy
// S = A/(4l_p²) = (k_B c³ A)/(4ħG)
// Hawking's 1974 discovery: S = A/4 in Planck units
double bekenstein_hawking_entropy(double area) {
    return K_B * C_CUBED * area / (4.0 * HBAR * G);
}

// Alternative form: S = 4πGM²k_B/(ħc)
double bh_entropy_from_mass(double mass) {
    return 4.0 * PI * G * mass * mass * K_B / (HBAR * C);
}

// Hawking's Equation 4: Hawking temperature
// T = ħc³/(8πGMk_B)
double hawking_temperature(double mass) {
    return HBAR * C_CUBED / (8.0 * PI * G * mass * K_B);
}

// Hawking's Equation 5: Surface gravity (κ)
// κ = c⁴/(4GM) = 1/(4M) in geometric units
double surface_gravity(double mass) {
    return C_FOURTH / (4.0 * G * mass);
}

// Hawking's Equation 6: Black hole luminosity (power emitted)
// P = dM/dt = -ħc⁴/(15360πG²M²) = -σAT⁴
double hawking_luminosity(double mass) {
    double area = horizon_area(mass);
    double temp = hawking_temperature(mass);
    return SIGMA * area * temp * temp * temp * temp;
}

// Alternative form: P = ħc⁶/(15360πG²M²)
double hawking_luminosity_direct(double mass) {
    return HBAR * C * C * C * C * C * C / 
           (15360.0 * PI * G_SQUARED * mass * mass);
}

// Hawking's Equation 7: Evaporation time
// τ = 5120πG²M³/(ħc⁴)
double evaporation_time(double mass) {
    return 5120.0 * PI * G_SQUARED * mass * mass * mass / 
           (HBAR * C_FOURTH);
}

// ============================================================================
// KERR BLACK HOLE (ROTATING)
// ============================================================================

// Hawking's Equation 8: Kerr metric parameters
typedef struct {
    double mass;
    double angular_momentum;
    double a;                 // Spin parameter a = J/(Mc)
    double r_plus;            // Outer event horizon
    double r_minus;           // Inner event horizon
    double ergosphere_radius; // Ergosphere boundary
} KerrBlackHole;

// Initialize Kerr black hole
KerrBlackHole create_kerr_black_hole(double mass, double angular_momentum) {
    KerrBlackHole kerr;
    kerr.mass = mass;
    kerr.angular_momentum = angular_momentum;
    kerr.a = angular_momentum / (mass * C);
    
    double M = mass;
    double a = kerr.a;
    double GM = G * M / C_SQUARED;  // Geometric mass
    
    // Event horizons: r± = GM ± √((GM)² - a²)
    double discriminant = GM * GM - a * a;
    
    if (discriminant >= 0) {
        kerr.r_plus = GM + sqrt(discriminant);
        kerr.r_minus = GM - sqrt(discriminant);
    } else {
        // Naked singularity
        kerr.r_plus = 0;
        kerr.r_minus = 0;
    }
    
    // Ergosphere: r = GM + √((GM)² - a²cos²θ)
    kerr.ergosphere_radius = GM + sqrt(GM * GM - a * a);
    
    return kerr;
}

// Hawking's Equation 9: Kerr black hole area
// A = 8πG²M²/c⁴ (1 + √(1 - a²/(GM)²))
double kerr_horizon_area(KerrBlackHole* kerr) {
    double M = kerr->mass;
    double a = kerr->a;
    double GM = G * M / C_SQUARED;
    
    return 8.0 * PI * GM * GM * (1.0 + sqrt(1.0 - (a*a)/(GM*GM)));
}

// Hawking's Equation 10: Kerr black hole temperature
// T = (ħc³√(GM)² - a²)/(4πGM[GM + √(GM)² - a²])
double kerr_hawking_temperature(KerrBlackHole* kerr) {
    double M = kerr->mass;
    double a = kerr->a;
    double GM = G * M / C_SQUARED;
    
    double numerator = HBAR * C_CUBED * sqrt(GM * GM - a * a);
    double denominator = 4.0 * PI * GM * (GM + sqrt(GM * GM - a * a));
    
    return numerator / (K_B * denominator);
}

// Hawking's Equation 11: Angular velocity of horizon
// Ω_H = a/(2GMr_+)
double kerr_angular_velocity(KerrBlackHole* kerr) {
    double M = kerr->mass;
    double a = kerr->a;
    double r_plus = kerr->r_plus;
    
    return a * C / (2.0 * G * M * r_plus / C_SQUARED);
}

// ============================================================================
// REISSNER-NORDSTRÖM BLACK HOLE (CHARGED)
// ============================================================================

typedef struct {
    double mass;
    double charge;
    double r_plus;
    double r_minus;
} ChargedBlackHole;

ChargedBlackHole create_charged_black_hole(double mass, double charge) {
    ChargedBlackHole rn;
    rn.mass = mass;
    rn.charge = charge;
    
    double M = mass;
    double Q = charge;
    double GM = G * M / C_SQUARED;
    double Q2 = Q * Q / (4.0 * PI * EPSILON_0 * C_CUBED * C);
    
    // Horizons: r± = GM ± √((GM)² - Q²)
    double discriminant = GM * GM - Q2;
    
    if (discriminant >= 0) {
        rn.r_plus = GM + sqrt(discriminant);
        rn.r_minus = GM - sqrt(discriminant);
    } else {
        // Naked singularity
        rn.r_plus = 0;
        rn.r_minus = 0;
    }
    
    return rn;
}

// Hawking's Equation 12: Charged black hole temperature
// T = ħc³√(GM)² - Q²/(2πk_B[GM + √(GM)² - Q²]²)
double charged_hawking_temperature(ChargedBlackHole* rn) {
    double M = rn->mass;
    double Q = rn->charge;
    double GM = G * M / C_SQUARED;
    double Q2 = Q * Q / (4.0 * PI * EPSILON_0 * C_CUBED * C);
    
    double sqrt_term = sqrt(GM * GM - Q2);
    double numerator = HBAR * C_CUBED * sqrt_term;
    double denominator = 2.0 * PI * K_B * pow(GM + sqrt_term, 2.0);
    
    return numerator / denominator;
}

// ============================================================================
// HAWKING RADIATION SPECTRUM
// ============================================================================

// Hawking's Equation 13: Greybody factor (simplified)
// Γ(ω) ≈ ω²/(exp(8πGMω/c³) - 1)
double greybody_factor(double frequency, double mass) {
    double omega = 2.0 * PI * frequency;
    double exponent = 8.0 * PI * G * mass * omega / (C_CUBED);
    
    if (exponent > 50.0) return 0.0;  // Avoid overflow
    
    return omega * omega / (exp(exponent) - 1.0);
}

// Hawking's Equation 14: Spectral energy density
// dE/dω = (ħω³)/(2π²c²) * Γ(ω)/(exp(ħω/k_BT) - 1)
double spectral_energy_density(double frequency, double mass) {
    double omega = 2.0 * PI * frequency;
    double T = hawking_temperature(mass);
    double gamma = greybody_factor(frequency, mass);
    
    double numerator = HBAR * omega * omega * omega * gamma;
    double denominator = 2.0 * PI_SQUARED * C_SQUARED * 
                        (exp(HBAR * omega / (K_B * T)) - 1.0);
    
    return numerator / denominator;
}

// Hawking's Equation 15: Total radiation power (integrated spectrum)
double total_radiation_power(double mass) {
    // Approximate integral: P ≈ ∑ ω * dE/dω * Δω
    double total = 0.0;
    int n_points = 1000;
    double max_freq = 10.0 * K_B * hawking_temperature(mass) / HBAR;
    
    for (int i = 1; i <= n_points; i++) {
        double freq = (double)i * max_freq / n_points;
        double dE = spectral_energy_density(freq, mass);
        total += dE * (max_freq / n_points);
    }
    
    return total;
}

// ============================================================================
// HAWKING'S INFORMATION PARADOX FUNCTIONS
// ============================================================================

// Hawking's Equation 16: Page curve (simplified)
// S_rad(t) = { S_BH(0) - S_BH(t) for t < t_page
//            { S_BH(t) for t > t_page
double page_curve_entropy(double time, double initial_mass) {
    double mass_at_time = mass_after_evaporation(initial_mass, time);
    double current_entropy = bekenstein_hawking_entropy(
        horizon_area(mass_at_time));
    double initial_entropy = bekenstein_hawking_entropy(
        horizon_area(initial_mass));
    
    double page_time = evaporation_time(initial_mass) / 2.0;
    
    if (time < page_time) {
        return initial_entropy - current_entropy;
    } else {
        return current_entropy;
    }
}

// Hawking's Equation 17: Mass evaporation over time
// M(t) = M₀(1 - t/τ)^(1/3)
double mass_after_evaporation(double initial_mass, double time) {
    double tau = evaporation_time(initial_mass);
    if (time >= tau) return 0.0;
    
    return initial_mass * pow(1.0 - time/tau, 1.0/3.0);
}

// Hawking's Equation 18: Information retrieval time (Page time)
// t_page = τ/2 = 2560πG²M³/(ħc⁴)
double page_time(double mass) {
    return evaporation_time(mass) / 2.0;
}

// ============================================================================
// ADVANCED HAWKING EQUATIONS
// ============================================================================

// Hawking's Equation 19: Black hole specific heat capacity
// C = dM/dT = -8πGM²k_B/(ħc)
double black_hole_specific_heat(double mass) {
    return -8.0 * PI * G * mass * mass * K_B / (HBAR * C);
}

// Hawking's Equation 20: Black hole thermodynamics First Law
// dM = TdS + ΩdJ + ΦdQ
double first_law_energy_change(double dS, double dJ, double dQ, 
                               BlackHole* bh) {
    double T = bh->temperature;
    double Omega = bh->omega;
    double Phi = G * bh->mass / (C_SQUARED * bh->schwarzschild_radius); // Electric potential
    
    return T * dS + Omega * dJ + Phi * dQ;
}

// Hawking's Equation 21: Black hole phase transitions
// For charged black holes: Critical point where heat capacity diverges
double critical_charge_ratio(double mass) {
    // Q_critical = M√G
    return sqrt(G) * mass;
}

// Hawking's Equation 22: Black hole evaporation rate (differential)
// dM/dt = -α/M² where α = ħc⁴/(5120πG²)
double evaporation_rate(double mass) {
    double alpha = HBAR * C_FOURTH / (5120.0 * PI * G_SQUARED);
    return -alpha / (mass * mass);
}

// ============================================================================
// COSMOLOGICAL APPLICATIONS
// ============================================================================

// Hawking's Equation 23: Primordial black hole formation
// Mass formed from horizon crossing: M ≈ c³t/(2G)
double primordial_mass_from_time(double time) {
    return C_CUBED * time / (2.0 * G);
}

// Hawking's Equation 24: Black hole in de Sitter space (with Λ)
// Temperature including cosmological constant: T = (κ/2π)(1 - Λr²/3)
double hawking_temperature_with_lambda(double mass) {
    double T0 = hawking_temperature(mass);
    double rs = schwarzschild_radius(mass);
    double correction = 1.0 - COSMOLOGICAL_LAMBDA * rs * rs / 3.0;
    
    return T0 * correction;
}

// ============================================================================
// QUANTUM GRAVITY CORRECTIONS
// ============================================================================

// Hawking's Equation 25: Logarithmic corrections to entropy
// S = A/4 - (3/2)ln(A) + constant
double entropy_with_log_corrections(double area) {
    double S0 = bekenstein_hawking_entropy(area);
    double A_in_planck_units = area / (L_PLANCK * L_PLANCK);
    
    return S0 / K_B - 1.5 * log(A_in_planck_units);
}

// Hawking's Equation 26: Backreaction corrections
// Corrected temperature including backreaction: T' = T(1 - αT²)
double temperature_with_backreaction(double mass, double alpha) {
    double T = hawking_temperature(mass);
    return T * (1.0 - alpha * T * T);
}

// ============================================================================
// NUMERICAL SIMULATION FUNCTIONS
// ============================================================================

// Simulate black hole evaporation over time
void simulate_evaporation(double initial_mass, double time_step, 
                         int num_steps) {
    printf("\n=== BLACK HOLE EVAPORATION SIMULATION ===\n");
    printf("Initial mass: %.2e kg (%.2f × M_Planck)\n", 
           initial_mass, initial_mass / M_PLANCK);
    printf("Time step: %.2e seconds\n", time_step);
    
    double mass = initial_mass;
    double total_time = 0.0;
    
    printf("\nTime (s)\t\tMass (kg)\t\tTemperature (K)\t\tLuminosity (W)\n");
    printf("--------------------------------------------------------------------------------\n");
    
    for (int i = 0; i < num_steps; i++) {
        if (mass <= M_PLANCK) break;
        
        double T = hawking_temperature(mass);
        double P = hawking_luminosity(mass);
        
        printf("%.2e\t\t%.2e\t\t%.2e\t\t%.2e\n", 
               total_time, mass, T, P);
        
        // Update mass: dm/dt = -P/c²
        mass -= (P / C_SQUARED) * time_step;
        total_time += time_step;
    }
    
    double tau = evaporation_time(initial_mass);
    printf("\nTheoretical evaporation time: %.2e seconds (%.2e years)\n",
           tau, tau / (365.25 * 24 * 3600));
    printf("Actual simulation time: %.2e seconds\n", total_time);
}

// Calculate Hawking radiation spectrum
RadiationSpectrum* calculate_spectrum(double mass, int num_points) {
    RadiationSpectrum* spectrum = (RadiationSpectrum*)malloc(
        num_points * sizeof(RadiationSpectrum));
    
    double max_freq = 10.0 * K_B * hawking_temperature(mass) / HBAR;
    
    for (int i = 0; i < num_points; i++) {
        double freq = (i + 1) * max_freq / num_points;
        spectrum[i].frequency = freq;
        spectrum[i].spectral_density = spectral_energy_density(freq, mass);
        spectrum[i].particle_flux = spectrum[i].spectral_density / (HBAR * 2.0 * PI * freq);
        spectrum[i].energy_flux = spectrum[i].spectral_density * C;
    }
    
    return spectrum;
}

// ============================================================================
// OUTPUT AND VISUALIZATION
// ============================================================================

void print_black_hole_properties(BlackHole* bh) {
    printf("\n=== BLACK HOLE PROPERTIES ===\n");
    printf("Mass:                    %.6e kg\n", bh->mass);
    printf("                         %.6f × M_Planck\n", bh->mass / M_PLANCK);
    printf("                         %.6f × M_Sun\n", bh->mass / 1.98847e30);
    
    printf("\nSchwarzschild radius:    %.6e m\n", bh->schwarzschild_radius);
    printf("                         %.6f × L_Planck\n", 
           bh->schwarzschild_radius / L_PLANCK);
    
    printf("\nEvent horizon area:      %.6e m²\n", bh->area);
    printf("                         %.6f × A_Planck\n", 
           bh->area / (L_PLANCK * L_PLANCK));
    
    printf("\nBekenstein-Hawking entropy: %.6e J/K\n", bh->entropy);
    printf("                         %.6e nats\n", bh->entropy / K_B);
    printf("Information content:     %.6e bits\n", 
           bh->entropy / (K_B * log(2.0)));
    
    printf("\nHawking temperature:     %.6e K\n", bh->temperature);
    printf("                         %.6f × T_Planck\n", 
           bh->temperature / T_PLANCK_K);
    
    printf("\nSurface gravity (κ):     %.6e m/s²\n", bh->surface_gravity);
    printf("                         %.6f × c⁴/(4GM)\n", 
           bh->surface_gravity * 4.0 * G * bh->mass / C_FOURTH);
    
    printf("\nHawking luminosity:      %.6e W\n", bh->luminosity);
    printf("                         %.6f × Solar luminosity\n", 
           bh->luminosity / 3.828e26);
    
    printf("\nEvaporation time:        %.6e seconds\n", bh->lifetime);
    printf("                         %.6e years\n", 
           bh->lifetime / (365.25 * 24 * 3600));
    printf("                         %.6e × age of universe\n", 
           bh->lifetime / (4.354e17));
    
    printf("\nSpecific heat capacity:  %.6e J/K\n", bh->specific_heat);
    printf("(Negative, indicating thermodynamic instability)\n");
}

void plot_spectrum(RadiationSpectrum* spectrum, int num_points) {
    printf("\n=== HAWKING RADIATION SPECTRUM ===\n");
    
    // Find maximum for normalization
    double max_density = 0.0;
    for (int i = 0; i < num_points; i++) {
        if (spectrum[i].spectral_density > max_density) {
            max_density = spectrum[i].spectral_density;
        }
    }
    
    int plot_width = 60;
    
    printf("Frequency (Hz)\t\tSpectral Density (arb. units)\n");
    printf("--------------------------------------------------------\n");
    
    for (int i = 0; i < num_points; i += num_points/20) {
        double density = spectrum[i].spectral_density;
        int bars = (int)((density / max_density) * plot_width);
        
        printf("%.2e\t", spectrum[i].frequency);
        for (int j = 0; j < bars; j++) {
            printf("█");
        }
        printf(" %.2e\n", density);
    }
}

// ============================================================================
// MAIN FUNCTION - DEMONSTRATE ALL HAWKING EQUATIONS
// ============================================================================

int main() {
    printf("================================================================\n");
    printf("STEPHEN HAWKING'S BLACK HOLE EQUATIONS - COMPLETE IMPLEMENTATION\n");
    printf("================================================================\n");
    
    // Define test black hole masses
    double test_masses[] = {
        M_PLANCK,                   // Planck mass
        1e-8 * M_PLANCK,            // Sub-Planckian
        1e3 * M_PLANCK,             // Small black hole
        1e10 * M_PLANCK,            // Primordial black hole
        1e15 * M_PLANCK,            // Asteroid mass
        1e38 * M_PLANCK,            // Stellar black hole (≈1 solar mass)
    };
    
    int num_tests = sizeof(test_masses) / sizeof(test_masses[0]);
    
    for (int i = 0; i < num_tests; i++) {
        double mass = test_masses[i];
        
        printf("\n\n=== TEST CASE %d: M = %.2e kg = %.2f × M_Planck ===\n", 
               i+1, mass, mass/M_PLANCK);
        
        // Calculate all properties
        BlackHole bh;
        bh.mass = mass;
        bh.schwarzschild_radius = schwarzschild_radius(mass);
        bh.area = horizon_area(mass);
        bh.entropy = bekenstein_hawking_entropy(bh.area);
        bh.temperature = hawking_temperature(mass);
        bh.surface_gravity = surface_gravity(mass);
        bh.luminosity = hawking_luminosity(mass);
        bh.lifetime = evaporation_time(mass);
        bh.specific_heat = black_hole_specific_heat(mass);
        bh.omega = 0;  // Non-rotating
        
        print_black_hole_properties(&bh);
        
        // Additional calculations for interesting cases
        if (mass <= 1e15 * M_PLANCK) {
            // Calculate radiation spectrum
            printf("\nCalculating Hawking radiation spectrum...\n");
            int spectrum_points = 100;
            RadiationSpectrum* spectrum = calculate_spectrum(mass, spectrum_points);
            plot_spectrum(spectrum, spectrum_points);
            free(spectrum);
            
            // Simulate evaporation for small black holes
            if (mass <= 1e10 * M_PLANCK) {
                simulate_evaporation(mass, bh.lifetime / 1000.0, 20);
            }
        }
        
        // Information paradox analysis
        if (i == 2) {  // For the 1e3 M_Planck case
            printf("\n=== INFORMATION PARADOX ANALYSIS ===\n");
            double page_t = page_time(mass);
            printf("Page time (information retrieval time): %.2e seconds\n", page_t);
            printf("Entropy at Page time: %.2e nats\n", 
                   page_curve_entropy(page_t, mass));
            
            // Calculate log corrections
            double S_corrected = entropy_with_log_corrections(bh.area);
            printf("Entropy with logarithmic corrections: %.6f nats\n", S_corrected);
        }
    }
    
    // Special case: Rotating (Kerr) black hole
    printf("\n\n=== KERR BLACK HOLE (ROTATING) ===\n");
    double kerr_mass = 1e38 * M_PLANCK;  // ~Solar mass
    double angular_momentum = 0.9 * kerr_mass * C * G * kerr_mass / (C_SQUARED);  // High spin
    
    KerrBlackHole kerr = create_kerr_black_hole(kerr_mass, angular_momentum);
    printf("Kerr black hole with spin parameter a = %.3f GM/c\n", kerr.a);
    printf("Outer horizon radius: %.6e m\n", kerr.r_plus);
    printf("Inner horizon radius: %.6e m\n", kerr.r_minus);
    printf("Ergosphere radius:    %.6e m\n", kerr.ergosphere_radius);
    printf("Horizon area:         %.6e m²\n", kerr_horizon_area(&kerr));
    printf("Hawking temperature:  %.6e K\n", kerr_hawking_temperature(&kerr));
    printf("Angular velocity:     %.6e rad/s\n", kerr_angular_velocity(&kerr));
    
    // Special case: Charged (Reissner-Nordström) black hole
    printf("\n\n=== REISSNER-NORDSTRÖM BLACK HOLE (CHARGED) ===\n");
    double rn_mass = 1e38 * M_PLANCK;
    double charge = 0.5 * critical_charge_ratio(rn_mass);  // Half of critical charge
    
    ChargedBlackHole rn = create_charged_black_hole(rn_mass, charge);
    printf("Charged black hole with Q = %.3f Q_max\n", 
           charge / critical_charge_ratio(rn_mass));
    printf("Outer horizon radius: %.6e m\n", rn.r_plus);
    printf("Inner horizon radius: %.6e m\n", rn.r_minus);
    printf("Hawking temperature:  %.6e K\n", charged_hawking_temperature(&rn));
    
    // Compare all three types
    printf("\n\n=== COMPARISON OF BLACK HOLE TYPES ===\n");
    printf("Type\t\t\tTemperature (K)\t\tEntropy (nats)\n");
    printf("--------------------------------------------------------\n");
    
    double comp_mass = 1e38 * M_PLANCK;
    
    // Schwarzschild
    double T_schw = hawking_temperature(comp_mass);
    double S_schw = bekenstein_hawking_entropy(horizon_area(comp_mass)) / K_B;
    printf("Schwarzschild\t\t%.2e\t\t%.2e\n", T_schw, S_schw);
    
    // Kerr (a = 0.9)
    KerrBlackHole comp_kerr = create_kerr_black_hole(comp_mass, 
        0.9 * comp_mass * C * G * comp_mass / (C_SQUARED));
    double T_kerr = kerr_hawking_temperature(&comp_kerr);
    double S_kerr = bekenstein_hawking_entropy(kerr_horizon_area(&comp_kerr)) / K_B;
    printf("Kerr (a=0.9)\t\t%.2e\t\t%.2e\n", T_kerr, S_kerr);
    
    // RN (Q = 0.5 Q_max)
    ChargedBlackHole comp_rn = create_charged_black_hole(comp_mass, 
        0.5 * critical_charge_ratio(comp_mass));
    double T_rn = charged_hawking_temperature(&comp_rn);
    double S_rn = bekenstein_hawking_entropy(
        FOUR_PI * comp_rn.r_plus * comp_rn.r_plus) / K_B;
    printf("RN (Q=0.5Q_max)\t\t%.2e\t\t%.2e\n", T_rn, S_rn);
    
    // Fundamental insights
    printf("\n\n=== HAWKING'S KEY INSIGHTS ===\n");
    printf("1. Black holes have temperature: T = ħκ/(2πk_B)\n");
    printf("2. Black holes radiate like black bodies: P = σAT⁴\n");
    printf("3. Black holes have entropy: S = A/(4l_p²)\n");
    printf("4. Information paradox: Pure states → mixed states?\n");
    printf("5. Black holes evaporate completely: τ ≈ G²M³/(ħc⁴)\n");
    
    // Calculate some interesting quantities
    printf("\n\n=== INTERESTING CALCULATIONS ===\n");
    
    // Black hole with 1-second lifetime
    double mass_1sec = pow(HBAR * C_FOURTH / (5120.0 * PI * G_SQUARED), 1.0/3.0);
    printf("Black hole with 1-second lifetime:\n");
    printf("  Mass: %.2e kg (%.1f × M_Planck)\n", mass_1sec, mass_1sec/M_PLANCK);
    printf("  Temperature: %.2e K\n", hawking_temperature(mass_1sec));
    printf("  Peak radiation wavelength: %.2e m\n", 
           H * C / (4.965 * K_B * hawking_temperature(mass_1sec)));
    
    // Black hole at CMB temperature
    double T_cmb = 2.725;  // Cosmic Microwave Background temperature
    double mass_cmb = HBAR * C_CUBED / (8.0 * PI * G * T_cmb * K_B);
    printf("\nBlack hole in equilibrium with CMB (T = %.3f K):\n", T_cmb);
    printf("  Mass: %.2e kg (%.1f × M_Earth)\n", 
           mass_cmb, mass_cmb / 5.972e24);
    printf("  Schwarzschild radius: %.2e m\n", schwarzschild_radius(mass_cmb));
    printf("  Such black holes would actually GROW by absorbing CMB radiation!\n");
    
    printf("\n================================================================\n");
    printf("END OF HAWKING BLACK HOLE EQUATIONS DEMONSTRATION\n");
    printf("================================================================\n");
    
    return 0;
}
