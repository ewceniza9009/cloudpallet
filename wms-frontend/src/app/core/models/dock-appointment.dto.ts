
export interface DockAppointmentDto {
  id: string;
  dockId: string;
  dockName: string;
  truckId: string;
  licensePlate: string;
  startDateTime: string;
  endDateTime: string;
  status: 'Scheduled' | 'InProgress' | 'Unloading' | 'Completed' | 'Cancelled';
  yardSpotNumber?: string;
}
