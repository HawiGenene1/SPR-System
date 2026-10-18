export interface ApiResponse<T = any> {
  success: boolean;
  message?: string;
  data: T;
  errors?: string[];
}

export type UserRole = 'Admin' | 'Attendant' | 'ParkingAttendant' | 'Customer';

export interface UserProfile {
  id: string;
  fullName: string;
  email: string;
  phoneNumber?: string;
  role: UserRole;
  createdAt: string;
  isActive: boolean;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  fullName: string;
  email: string;
  password: string;
  phoneNumber?: string;
}

export interface AuthResponse {
  token: string;
  expiresAt: string;
  user: UserProfile;
}

export interface VehicleDto {
  id: string;
  userId: string;
  licensePlate: string;
  make: string;
  model: string;
  color: string;
  vehicleType: string;
  createdAt: string;
}

export interface CreateVehicleRequest {
  licensePlate: string;
  make: string;
  model: string;
  color: string;
  vehicleType: string;
}

export interface ParkingAreaDto {
  id: string;
  name: string;
  location: string;
  hourlyRate: number;
  totalSlots: number;
  availableSlots: number;
  isActive: boolean;
  createdAt: string;
}

export interface CreateParkingAreaRequest {
  name: string;
  location: string;
  hourlyRate: number;
  totalSlots: number;
  isActive?: boolean;
}

export type SlotStatus = 'Available' | 'Reserved' | 'Occupied' | 'Maintenance';
export type SlotType = 'Standard' | 'EV' | 'Disabled' | 'VIP';

export interface ParkingSlotDto {
  id: string;
  parkingAreaId: string;
  parkingAreaName?: string;
  slotNumber: string;
  floorNumber?: number;
  slotType: SlotType | string;
  vehicleType?: string;
  status: SlotStatus | string;
  statusName?: string;
  pricePerHour?: number;
  hourlyRate?: number;
  isActive: boolean;
  isAvailable?: boolean;
}

export interface CreateParkingSlotRequest {
  parkingAreaId: string;
  slotNumber: string;
  slotType: string;
}

export type ReservationStatus = 'Pending' | 'Confirmed' | 'Active' | 'Completed' | 'Cancelled';

export interface ReservationDto {
  id: string;
  userId: string;
  userName?: string;
  userEmail?: string;
  vehicleId: string;
  vehiclePlate?: string;
  parkingSlotId: string;
  slotNumber?: string;
  parkingAreaId?: string;
  parkingAreaName?: string;
  startTime: string;
  endTime: string;
  totalAmount: number;
  status: ReservationStatus | string;
  createdAt: string;
  payment?: PaymentDto;
  qrCode?: QrCodeDto;
}

export interface CreateReservationRequest {
  vehicleId: string;
  parkingSlotId: string;
  startTime: string;
  endTime: string;
}

export interface CheckAvailabilityRequest {
  parkingSlotId: string;
  startTime: string;
  endTime: string;
}

export interface AvailabilityResultDto {
  isAvailable: boolean;
  estimatedPrice: number;
  message?: string;
}

export type PaymentMethod = 'CreditCard' | 'DebitCard' | 'DigitalWallet' | 'Cash';
export type PaymentStatus = 'Pending' | 'Completed' | 'Failed' | 'Refunded';

export interface PaymentDto {
  id: string;
  reservationId: string;
  amount: number;
  paymentMethod: PaymentMethod | string;
  status: PaymentStatus | string;
  transactionId?: string;
  paidAt?: string;
  createdAt?: string;
}

export interface ProcessPaymentRequest {
  reservationId: string;
  paymentMethod: string;
  amount: number;
}

export interface QrCodeDto {
  id: string;
  reservationId: string;
  code: string;
  imageData?: string;
  qrCodeData?: string;
  reservationNumber?: string;
  status?: 'Active' | 'Used' | 'Expired' | string;
  expiresAt: string;
  generatedAt?: string;
  createdAt?: string;
}

export interface CheckInRequest {
  qrCode: string;
  attendantNotes?: string;
}

export interface CheckOutRequest {
  qrCode: string;
  attendantNotes?: string;
}

export interface CheckInResultDto {
  success: boolean;
  message: string;
  checkInTime: string;
  slotNumber: string;
  licensePlate: string;
  reservationId: string;
}

export interface CheckOutResultDto {
  success: boolean;
  message: string;
  checkInTime: string;
  checkOutTime: string;
  durationMinutes: number;
  overstayMinutes: number;
  overstayFee: number;
  totalDue: number;
  requiresAdditionalPayment: boolean;
}

export interface ParkingRecordDto {
  id: string;
  reservationId: string;
  slotNumber: string;
  parkingAreaName: string;
  licensePlate: string;
  customerName: string;
  checkInTime: string;
  checkOutTime?: string;
  durationMinutes?: number;
  overstayMinutes?: number;
  overstayFee?: number;
  status: string;
}

export interface UserDto {
  id: string;
  fullName: string;
  email: string;
  phoneNumber?: string;
  isActive: boolean;
  roleId?: string;
  roleName: string;
  assignedParkingAreaId?: string;
  assignedParkingAreaName?: string;
  createdAt: string;
  updatedAt?: string;
}

export interface UpdateUserRequest {
  fullName: string;
  phoneNumber?: string;
  assignedParkingAreaId?: string;
  isActive?: boolean;
}

export interface SystemSettingsDto {
  systemName: string;
  supportEmail: string;
  contactPhone: string;
  currency: string;
  currencySymbol: string;
  taxRatePercentage: number;
  gracePeriodMinutes: number;
  hourlyOverstayFee: number;
  maxAdvanceBookingDays: number;
  minBookingDurationMinutes: number;
  autoCancelUnattendedMinutes: number;
  requirePrepayment: boolean;
  maintenanceMode: boolean;
  enableFastPassBarriers: boolean;
  enableEmailReceipts: boolean;
}

export interface AdminDashboardDto {
  totalUsers?: number;
  totalCustomers?: number;
  totalAttendants?: number;
  totalParkingAreas: number;
  totalSlots: number;
  totalParkingSlots?: number;
  availableSlots: number;
  occupiedSlots: number;
  reservedSlots: number;
  maintenanceSlots?: number;
  occupancyRate: number;
  occupancyRatePercentage?: number;
  todaysReservations?: number;
  todayReservations: number;
  activeReservations?: number;
  completedReservations?: number;
  cancelledReservations?: number;
  todaysRevenue?: number;
  todayRevenue: number;
  monthlyRevenue?: number;
  totalRevenue: number;
  activeCheckIns: number;
  revenueByDay?: Array<{ date: string; dateLabel: string; revenue: number; transactionsCount: number }>;
  reservationsByDay?: Array<{ date: string; dateLabel: string; totalReservations: number; completedCount: number; cancelledCount: number; activeCount: number }>;
  occupancyStatistics?: {
    overallOccupancyRatePercentage: number;
    totalSlots: number;
    occupiedSlots: number;
    reservedSlots: number;
    availableSlots: number;
    maintenanceSlots: number;
    byParkingArea?: Array<{ parkingAreaId: string; parkingAreaName: string; totalSlots: number; occupiedSlots: number; availableSlots: number; occupancyRatePercentage: number }>;
  };
  recentReservations?: ReservationDto[];
}

export interface AttendantDashboardDto {
  assignedParkingAreaId?: string;
  assignedParkingAreaName?: string;
  assignedArea?: string;
  facilityAddress?: string;
  totalSlots: number;
  availableSlots: number;
  occupiedSlots: number;
  reservedSlots: number;
  maintenanceSlots?: number;
  occupancyRatePercentage: number;
  todaysCheckIns?: number;
  todayCheckIns: number;
  checkInsToday?: number;
  todaysCheckOuts?: number;
  todayCheckOuts: number;
  checkOutsToday?: number;
  activeParkedVehicles?: number;
  activeReservations?: number;
  upcomingArrivals?: number;
  pendingArrivalsToday?: number;
  recentVehicleEntries?: any[];
  recentRecords?: ParkingRecordDto[];
}

export interface ScannedQRResult {
  qrCode: string;
  isValid: boolean;
  isExpired: boolean;
  isUsedForEntry: boolean;
  entryScannedAt?: string;
  isUsedForExit: boolean;
  exitScannedAt?: string;
  expiresAt: string;
  reservation: {
    id: string;
    reservationNumber?: string;
    customerName: string;
    customerPhone?: string;
    licensePlate: string;
    vehicle: string;
    parkingArea: string;
    slotNumber: string;
    floorNumber?: number;
    startTime: string;
    endTime: string;
    status: string;
  };
}

export interface CustomerDashboardDto {
  upcomingReservation?: ReservationDto | null;
  activeReservation?: ReservationDto | null;
  totalReservations: number;
  completedReservations: number;
  activeReservationsCount?: number;
  upcomingReservationsCount?: number;
  cancelledReservationsCount?: number;
  pastReservationsCount?: number;
  totalPayments: number;
  totalSpent?: number;
  totalPaymentsCount?: number;
  registeredVehicles?: VehicleDto[];
  registeredVehiclesCount?: number;
  vehicles?: VehicleDto[];
  recentParkingHistory?: ParkingRecordDto[];
  recentReservations?: ReservationDto[];
  // Backwards compatibility aliases:
  activeReservations?: ReservationDto[];
  completedParkingsCount?: number;
}

export interface RevenueReportDto {
  totalRevenue: number;
  dates: string[];
  amounts: number[];
  byMethod?: { [key: string]: number };
}

export interface OccupancyReportDto {
  labels: string[];
  occupancyRates: number[];
  peakHour: string;
  averageDurationHours: number;
}

export interface PagedResultDto<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

export interface ReportFilterRequest {
  period?: 'daily' | 'weekly' | 'monthly' | 'custom' | string;
  startDate?: string;
  endDate?: string;
  parkingAreaId?: string;
  search?: string;
  pageNumber?: number;
  pageSize?: number;
}

export interface ReservationReportDto {
  totalReservations: number;
  confirmedCount: number;
  activeCount: number;
  completedCount: number;
  cancelledCount: number;
  completionRatePercentage: number;
  cancellationRatePercentage: number;
  dates: string[];
  counts: number[];
  reservations?: ReservationDto[];
}

export interface CustomerReportDto {
  totalCustomers: number;
  activeCustomers: number;
  newCustomersThisMonth: number;
  topSpenders?: Array<{ customerId: string; customerName: string; totalSpent: number; totalReservations: number }>;
  customers?: UserDto[];
}

