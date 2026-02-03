// physics.gleam - Physics namespace with types and functions

// Define a public type for 2D coordinates
pub type Coordinate {
  Coordinate(x: Float, y: Float)
}

// Define a public type for 3D vectors
pub type Vector3D {
  Vector3D(x: Float, y: Float, z: Float)
}

// Define a public type for different physics systems
pub type PhysicsSystem {
  Classical
  Quantum
  Relativistic
  Thermodynamic
}

// Define a public type for calculation results
pub type CalculationResult {
  Result(value: Float, unit: String, system: PhysicsSystem)
  Error(message: String)
}

// Define a public interface type (using Gleam's opaque types)
pub opaque type PhysicsInterface {
  PhysicsInterface(
    name: String,
    version: String,
    calculate: fn(PhysicsSystem, List(Float)) -> CalculationResult,
    validate: fn(CalculationResult) -> Bool
  )
}

// Public functions for the Physics namespace

/// Creates a new coordinate
pub fn create_coordinate(x: Float, y: Float) -> Coordinate {
  Coordinate(x: x, y: y)
}

/// Creates a new 3D vector
pub fn create_vector(x: Float, y: Float, z: Float) -> Vector3D {
  Vector3D(x: x, y: y, z: z)
}

/// Calculate distance between two coordinates
pub fn distance(coord1: Coordinate, coord2: Coordinate) -> Float {
  let dx = coord1.x - coord2.x
  let dy = coord1.y - coord2.y
  float.sqrt(dx *. dx +. dy *. dy)
}

/// Calculate magnitude of a 3D vector
pub fn magnitude(vec: Vector3D) -> Float {
  float.sqrt(vec.x *. vec.x +. vec.y *. vec.y +. vec.z *. vec.z)
}

/// Add two vectors
pub fn add_vectors(vec1: Vector3D, vec2: Vector3D) -> Vector3D {
  Vector3D(
    x: vec1.x +. vec2.x,
    y: vec1.y +. vec2.y,
    z: vec1.z +. vec2.z
  )
}

/// Calculate kinetic energy (1/2 * m * v^2)
pub fn kinetic_energy(mass: Float, velocity: Float) -> CalculationResult {
  let value = 0.5 *. mass *. velocity *. velocity
  Result(value: value, unit: "Joules", system: Classical)
}

/// Calculate gravitational force (F = G * m1 * m2 / r^2)
pub fn gravitational_force(m1: Float, m2: Float, r: Float) -> CalculationResult {
  let g = 6.67430e-11  // gravitational constant
  let value = g *. m1 *. m2 /. (r *. r)
  Result(value: value, unit: "Newtons", system: Classical)
}

/// Calculate energy from mass (E = mc^2)
pub fn mass_energy_equivalence(mass: Float) -> CalculationResult {
  let c = 299792458.0  // speed of light
  let value = mass *. c *. c
  Result(value: value, unit: "Joules", system: Relativistic)
}

/// Create a new physics interface
pub fn create_interface(
  name: String,
  version: String,
  calculate_fn: fn(PhysicsSystem, List(Float)) -> CalculationResult,
  validate_fn: fn(CalculationResult) -> Bool
) -> PhysicsInterface {
  PhysicsInterface(
    name: name,
    version: version,
    calculate: calculate_fn,
    validate: validate_fn
  )
}

/// Use the interface to perform a calculation
pub fn use_interface(
  interface: PhysicsInterface,
  system: PhysicsSystem,
  inputs: List(Float)
) -> CalculationResult {
  interface.calculate(system, inputs)
}

/// Validate a calculation result using the interface
pub fn validate_result(
  interface: PhysicsInterface,
  result: CalculationResult
) -> Bool {
  interface.validate(result)
}

/// Get interface information
pub fn get_interface_info(interface: PhysicsInterface) -> String {
  "Interface: " <> interface.name <> " v" <> interface.version
}

/// Example calculation function for the interface
pub fn example_calculator(
  system: PhysicsSystem,
  inputs: List(Float)
) -> CalculationResult {
  case system, inputs {
    Classical, [mass, velocity] -> kinetic_energy(mass, velocity)
    Classical, [m1, m2, r] -> gravitational_force(m1, m2, r)
    Relativistic, [mass] -> mass_energy_equivalence(mass)
    _, _ -> Error(message: "Invalid inputs for system")
  }
}

/// Example validation function
pub fn example_validator(result: CalculationResult) -> Bool {
  case result {
    Result(value, _, _) -> value >. 0.0
    Error(_) -> False
  }
}
