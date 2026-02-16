import { Component, inject, OnInit, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { NgSelectModule } from '@ng-select/ng-select';
import { DashboardService } from '../../services/dashboard';
import { AuthService } from '../../services/auth';
import { StatCardComponent } from '../../shared/components/stat-card/stat-card';
import { forkJoin, firstValueFrom } from 'rxjs';
import { DASHBOARD_LABELS, DASHBOARD_MESSAGES } from '../../shared/constants/messages';

declare var Chart: any;

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, StatCardComponent, NgSelectModule],
  templateUrl: './admin-dashboard.html',
  styleUrl: './admin-dashboard.scss'
})
export class AdminDashboardComponent implements OnInit {
  private dashboardService = inject(DashboardService);
  private authService = inject(AuthService);

  @ViewChild('myChart') chartCanvas!: ElementRef<HTMLCanvasElement>;
  @ViewChild('breakdownChart') breakdownCanvas!: ElementRef<HTMLCanvasElement>;
  private chart: any;
  private pieChart: any;

  get selectedAppName(): string {
    const selected = this.emailCount.find(x => x.appId === this.selectedAppId);
    return selected?.appName || 'All';
  }

  getTopApps(): any[] {
    return this.emailCount.filter(x => x.appId !== 0).sort((a, b) => b.totalSentEmail - a.totalSentEmail).slice(0, 4);
  }

  getTop10Apps(): any[] {
    return this.top10Apps.slice(0, 5);
  }

  getAppPercentage(value: number): number {
    const max = Math.max(...this.emailCount.map(x => x.totalSentEmail || 0));
    return max > 0 ? (value / max) * 100 : 0;
  }

  userProfile: any = null;
  totalSentEmail = 0;
  monthlyEmail = 0;
  todayAllEmail = 0;
  lastSevenDaysEmail = 0;
  lastThirtyDaysEmail = 0;
  yearlyEmail = 0;
  totalAppUser = 0;
  activeAppUser = 0;
  inactiveAppUser = 0;
  emailCount: any[] = [];
  top10Apps: any[] = [];
  selectedAppId: number = 0;
  currentMonthName = new Intl.DateTimeFormat('en-US', { month: 'long' }).format(new Date());
  isLoading = true;

  async ngOnInit(): Promise<void> {
    const userId = await this.authService.getUserId();
    if (!userId) {
      // Set default empty profile so dashboard renders
      this.userProfile = {
        email: 'Admin',
        appName: 'No applications',
        creationDateTime: new Date()
      };
      this.initEmptyState();
      this.isLoading = false;
      return;
    }

    try {
      const results = await firstValueFrom(forkJoin({
        profile: this.dashboardService.getUserProfile(userId),
        dashboard: this.dashboardService.getAdminDashboardData(),
        top10: this.dashboardService.getTop10Apps(),
        totalUsers: this.dashboardService.getRowsCount('AppUser'),
        activeUsers: this.dashboardService.getRowsCount('AppUser', 'Active = 1'),
        userInfo: this.authService.getUserInfo()
      }));

      this.userProfile = results.profile.resultData?.[0] || {
        email: 'Admin',
        appName: 'No applications',
        creationDateTime: new Date()
      };
      
      // Get user info from localStorage (set during Azure AD authentication)
      const userInfoStr = localStorage.getItem('userInfo');
      if (userInfoStr) {
        try {
          const userInfo = JSON.parse(userInfoStr);
          if (this.userProfile) {
            // Use email from localStorage which has the actual email address, not UPN
            this.userProfile.email = userInfo.mail || userInfo.email;
            this.userProfile.givenName = userInfo.givenName || '';
            this.userProfile.surname = userInfo.surname || '';

            // Prefer displayName from SearchADUsers; fallback to givenName + surname
            const fullName = `${this.userProfile.givenName} ${this.userProfile.surname}`.trim();
            this.userProfile.displayName = (userInfo.displayName || '').trim() || fullName;
          }
        } catch (e) {
          console.warn('Error parsing userInfo from localStorage:', e);
        }
      }
      
      this.emailCount = results.dashboard.resultData || [];
      this.top10Apps = (results.top10?.resultData || []).filter((x: any) => x.totalSentEmail > 0);
      this.totalAppUser = results.totalUsers.resultData?.[0] || 0;
      this.activeAppUser = results.activeUsers.resultData?.[0] || 0;
      this.inactiveAppUser = this.totalAppUser - this.activeAppUser;

      // Always initialize with default empty "All" option if no data
      if (!this.emailCount || this.emailCount.length === 0) {
        this.initEmptyState();
      } else {
        this.onchangeApp(0);
      }
      
      // Always initialize charts (with or without data)
      setTimeout(() => {
        this.initBarChart();
        this.initBreakdownChart();
      }, 150);
      
      this.isLoading = false;
    } catch (error) {
      // Set default values so dashboard still renders
      this.userProfile = {
        email: 'Admin',
        appName: 'No applications',
        creationDateTime: new Date()
      };
      this.initEmptyState();
      setTimeout(() => {
        this.initBarChart();
        this.initBreakdownChart();
      }, 150);
      this.isLoading = false;
    }
  }

  private initEmptyState() {
    this.emailCount = [{
      appId: 0,
      appName: DASHBOARD_LABELS.ALL_APPLICATIONS,
      todayEmail: 0,
      lastSevenDaysEmail: 0,
      lastThirtyDaysEmail: 0,
      monthlyEmail: 0,
      yearlyEmail: 0,
      totalSentEmail: 0
    }];
    this.onchangeApp(0);
  }

  onchangeApp(appId: any) {
    if (appId && typeof appId === 'object') {
      this.selectedAppId = +appId.appId;
    } else {
      this.selectedAppId = +appId;
    }
    const appData = this.emailCount.find(x => x.appId === this.selectedAppId);
    if (appData) {
      this.totalSentEmail = appData.totalSentEmail || 0;
      this.todayAllEmail = appData.todayEmail || 0;
      this.monthlyEmail = appData.monthlyEmail || 0;
      this.lastSevenDaysEmail = appData.lastSevenDaysEmail || 0;
      this.lastThirtyDaysEmail = appData.lastThirtyDaysEmail || 0;
      this.yearlyEmail = appData.yearlyEmail || 0;
    }
    // Update breakdown chart on selection change
    setTimeout(() => this.initBreakdownChart(), 0);
  }

  initBarChart() {
    const canvas = this.chartCanvas?.nativeElement;
    if (!canvas) {
      // Fallback to getElementById if ViewChild is not ready
      const fallbackCanvas = document.getElementById('myChart') as HTMLCanvasElement;
      if (!fallbackCanvas) return;
      this.createBarChart(fallbackCanvas);
    } else {
      this.createBarChart(canvas);
    }
  }

  private createBarChart(canvas: HTMLCanvasElement) {
    if (this.chart) {
      this.chart.destroy();
    }

    const barCharData = this.emailCount.filter(x => x.appId !== 0).sort((a, b) => a.appName.localeCompare(b.appName));
    
    // If no applications, we might want to show the "ALL" data or at least empty chart
    const labels = barCharData.length > 0 ? barCharData.map(x => x.appName) : [DASHBOARD_LABELS.BAR_LABEL_NO_DATA];
    const emails = barCharData.length > 0 ? barCharData.map(x => x.totalSentEmail) : [0];

    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    if (typeof Chart === 'undefined') {
      return;
    }

    // Create gradient colors for a premium look
    const gradients = labels.map((_, i) => {
      const g = ctx.createLinearGradient(0, 0, canvas.width, 0);
      const hue = (i * 37) % 360;
      g.addColorStop(0, `hsla(${hue}, 85%, 60%, 0.95)`);
      g.addColorStop(1, `hsla(${(hue + 25) % 360}, 85%, 45%, 0.95)`);
      return g;
    });

    this.chart = new Chart(ctx, {
      type: 'bar',
      data: {
        labels,
        datasets: [{
          label: DASHBOARD_LABELS.BAR_LABEL_SENT_EMAIL,
          data: emails,
          backgroundColor: gradients,
          borderRadius: 8,
          borderSkipped: false,
          barThickness: 18,
          maxBarThickness: 22
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        indexAxis: 'y',
        animation: {
          duration: 900,
          easing: 'easeOutQuart'
        },
        plugins: {
          tooltip: {
            enabled: true,
            callbacks: {
              label: (context: any) => ` Total: ${context.parsed.x.toLocaleString()}`
            }
          },
          legend: {
            display: false
          },
          title: {
            display: barCharData.length === 0,
            text: DASHBOARD_LABELS.BAR_LABEL_NO_DATA
          }
        },
        scales: {
          x: {
            grid: {
              color: 'rgba(0,0,0,0.05)'
            },
            ticks: {
              color: '#6c757d'
            }
          },
          y: {
            grid: {
              display: false
            },
            ticks: {
              color: '#343a40'
            }
          }
        }
      }
    });
  }

  initBreakdownChart() {
    const canvas = this.breakdownCanvas?.nativeElement || (document.getElementById('breakdownChart') as HTMLCanvasElement);
    if (!canvas) return;
    this.createBreakdownChart(canvas);
  }

  private createBreakdownChart(canvas: HTMLCanvasElement) {
    if (this.pieChart) {
      this.pieChart.destroy();
    }

    const ctx = canvas.getContext('2d');
    if (!ctx || typeof Chart === 'undefined') return;

    const selected = this.emailCount.find(x => x.appId === this.selectedAppId) || {
      todayEmail: 0,
      lastSevenDaysEmail: 0,
      lastThirtyDaysEmail: 0,
      monthlyEmail: 0,
      yearlyEmail: 0
    };

    const labels = ['Today', '7 Days', '30 Days', 'Monthly', 'Yearly'];
    const data = [
      selected.todayEmail || 0,
      selected.lastSevenDaysEmail || 0,
      selected.lastThirtyDaysEmail || 0,
      selected.monthlyEmail || 0,
      selected.yearlyEmail || 0
    ];

    const baseColors = [
      'rgba(54, 162, 235, 0.9)',
      'rgba(255, 206, 86, 0.9)',
      'rgba(75, 192, 192, 0.9)',
      'rgba(153, 102, 255, 0.9)',
      'rgba(255, 99, 132, 0.9)'
    ];

    this.pieChart = new Chart(ctx, {
      type: 'doughnut',
      data: {
        labels,
        datasets: [{
          data,
          backgroundColor: baseColors,
          borderColor: '#ffffff',
          borderWidth: 2
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        cutout: '58%',
        plugins: {
          legend: {
            position: 'bottom'
          },
          tooltip: {
            callbacks: {
              label: (context: any) => ` ${context.label}: ${context.parsed.toLocaleString()}`
            }
          }
        }
      }
    });
  }
}