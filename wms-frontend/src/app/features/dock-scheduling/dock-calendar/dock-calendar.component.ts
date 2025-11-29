import { Component, ChangeDetectionStrategy, signal, inject, OnInit, Input, OnChanges, SimpleChanges, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FullCalendarModule } from '@fullcalendar/angular';
import { CalendarOptions, EventInput, EventClickArg, DateSelectArg } from '@fullcalendar/core';
import dayGridPlugin from '@fullcalendar/daygrid';
import timeGridPlugin from '@fullcalendar/timegrid';
import interactionPlugin from '@fullcalendar/interaction';
import { DockAppointmentDto } from '../../../core/models/dock-appointment.dto';

@Component({
  selector: 'app-dock-calendar',
  standalone: true,
  imports: [CommonModule, FullCalendarModule],
  template: `
    <div class="calendar-container">
      <full-calendar [options]="calendarOptions()"></full-calendar>
    </div>
  `,
  styleUrls: ['./dock-calendar.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DockCalendarComponent implements OnInit, OnChanges {
  @Input() appointments: DockAppointmentDto[] = [];
  @Output() dateRangeChanged = new EventEmitter<{ start: Date, end: Date }>();
  @Output() eventClicked = new EventEmitter<DockAppointmentDto>();
  @Output() appointmentRescheduled = new EventEmitter<{ appointment: DockAppointmentDto, newStart: Date, newEnd: Date }>();

  calendarOptions = signal<CalendarOptions>({
    plugins: [dayGridPlugin, timeGridPlugin, interactionPlugin],
    initialView: 'timeGridDay',
    headerToolbar: {
      left: 'prev,next today',
      center: 'title',
      right: 'dayGridMonth,timeGridWeek,timeGridDay'
    },
    weekends: true,
    editable: true, // Enable drag and drop
    selectable: true,
    selectMirror: true,
    dayMaxEvents: true,
    slotMinTime: '00:00:00',
    slotMaxTime: '24:00:00',
    height: 'auto',
    datesSet: (arg) => {
      this.dateRangeChanged.emit({ start: arg.start, end: arg.end });
    },
    eventClick: (arg) => this.handleEventClick(arg),
    eventDrop: (arg) => this.handleEventDrop(arg),
    eventResize: (arg) => this.handleEventResize(arg),
    eventContent: (arg) => {
      return {
        html: `
          <div class="fc-content">
            <div class="fc-header">
              <div class="fc-title">${arg.event.title || 'No Plate'}</div>
            </div>
            <div class="fc-body">
              <div class="fc-desc">${arg.event.extendedProps['dockName']}</div>
              <div class="fc-status status-${(arg.event.extendedProps['status'] || 'scheduled').toLowerCase()}">${arg.event.extendedProps['status'] || 'Scheduled'}</div>
            </div>
          </div>
        `
      };
    }
  });

  ngOnInit() {
    this.updateEvents();
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['appointments']) {
      this.updateEvents();
    }
  }

  private updateEvents() {
    const events: EventInput[] = this.appointments.map(apt => {
      const colors = this.getStatusColors(apt.status);
      return {
        id: apt.id,
        title: apt.licensePlate || 'Unknown',
        start: apt.startDateTime,
        end: apt.endDateTime,
        backgroundColor: colors.bg,
        borderColor: colors.border,
        textColor: colors.text,
        extendedProps: {
          dockName: apt.dockName,
          status: apt.status,
          original: apt
        },
        editable: apt.status === 'Scheduled' // Only allow rescheduling if Scheduled
      };
    });

    this.calendarOptions.update(options => ({
      ...options,
      events: events
    }));
  }

  private getStatusColors(status: string): { bg: string, border: string, text: string } {
    switch (status) {
      case 'Scheduled': 
        return { bg: '#e8eaf6', border: '#3f51b5', text: '#1a237e' }; // Indigo
      case 'Arrived': 
        return { bg: '#fff3e0', border: '#ff9800', text: '#e65100' }; // Orange
      case 'Loading': 
        return { bg: '#e8f5e9', border: '#4caf50', text: '#1b5e20' }; // Green
      case 'Completed': 
        return { bg: '#f5f5f5', border: '#9e9e9e', text: '#424242' }; // Grey
      case 'Cancelled': 
        return { bg: '#ffebee', border: '#f44336', text: '#b71c1c' }; // Red
      default: 
        return { bg: '#e8eaf6', border: '#3f51b5', text: '#1a237e' };
    }
  }

  private handleEventClick(clickInfo: EventClickArg) {
    this.eventClicked.emit(clickInfo.event.extendedProps['original']);
  }

  private handleEventDrop(dropInfo: any) {
    const appointment = dropInfo.event.extendedProps['original'];
    const newStart = dropInfo.event.start;
    const newEnd = dropInfo.event.end;
    this.appointmentRescheduled.emit({ appointment, newStart, newEnd });
  }

  private handleEventResize(resizeInfo: any) {
    const appointment = resizeInfo.event.extendedProps['original'];
    const newStart = resizeInfo.event.start;
    const newEnd = resizeInfo.event.end;
    this.appointmentRescheduled.emit({ appointment, newStart, newEnd });
  }
}
