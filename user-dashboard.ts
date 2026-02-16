import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { RouterModule } from '@angular/router';
import { DomSanitizer, SafeUrl } from '@angular/platform-browser';
import { DashboardService } from '../../services/dashboard';
import { AuthService } from '../../services/auth';
import { ApplicationService } from '../../services/application';
import { forkJoin, firstValueFrom } from 'rxjs';
import { DASHBOARD_LABELS } from '../../shared/constants/messages';
import { IMAGE_PATHS } from '../../shared/constants/image-paths';

declare var Chart: any;

@Component({
  selector: 'app-user-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, NgSelectModule, RouterModule],
  templateUrl: './user-dashboard.html',
  styleUrl: './user-dashboard.scss'
})
export class UserDashboardComponent implements OnInit {
  private dashboardService = inject(DashboardService);
  private authService = inject(AuthService);
  private applicationService = inject(ApplicationService);
  private sanitizer = inject(DomSanitizer);

  public readonly IMAGE_PATHS = IMAGE_PATHS;
  userProfileImage: SafeUrl | string = IMAGE_PATHS.USER_PROFILE;

  private chart: any;
  private pieChart: any;
  chartType: 'bar' | 'line' = 'bar';

  changeChartType(type: 'bar' | 'line') {
    this.chartType = type;
    this.initChart();
  }

  userProfile: any = null;
  unverifiedApplications: any[] = [];
  appList: any[] = [];
  totalSentEmail = 0;
  monthlyEmail = 0;
  todayAllEmail = 0;
  lastSevenDaysEmail = 0;
  lastThirtyDaysEmail = 0;
  yearlyEmail = 0;
  totalApplications = 0;
  emailCount: any[] = []; // Individual user data for stats and dropdown
  globalChartData: any[] = []; // Global data for graphs
  top10Apps: any[] = [];
  selectedAppIds: number[] = []; // Changed to array for multi-select

  get selectedAppName(): string {
    if (this.selectedAppIds && this.selectedAppIds.length > 0) {
      return this.selectedAppIds.map(id => {
        const app = this.globalChartData.find(x => x.appId === id);
        return app?.appName || '';
      }).join(', ');
    }
    return 'All';
  }

  getTop10Apps(): any[] {
    return this.top10Apps;
  }

  async ngOnInit(): Promise<void> {
    const token = this.authService.getTokenFromCache();
    if (!token) return;
    const payload = JSON.parse(atob(token.split('.')[1]));
    const userId = payload['nameid'] || payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'];

    try {
      const results: any = await firstValueFrom(forkJoin({
        profile: this.dashboardService.getUserProfile(userId),
        dashboard: this.dashboardService.getAdminDashboardData(userId), // Individual user stats
        globalDashboard: this.dashboardService.getAdminDashboardData(), // Global stats for graphs
        top10: this.dashboardService.getTop10Apps(),
        userInfo: this.authService.getUserInfo(),
        applications: this.applicationService.getUserApplications(userId, 1, 100),
        globalApplications: this.applicationService.getApplications(1, 100) // Get all global apps with full details
      }));

      // Get application list (show only top 5), but keep total from server
      this.appList = (results.applications?.data || []).slice(0, 5);

      // Handle profile - may be in data array or directly
      const profileData = results.profile?.resultData?.[0] || results.profile;
      this.userProfile = profileData;
      
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

      // Fetch profile photo if UPN is available
      if (this.userProfile && this.userProfile.upn) {
        try {
          const photoResponse = await firstValueFrom(
            this.applicationService.getADUserPhoto(this.userProfile.upn)
          );
          console.log('Dashboard - Photo response:', photoResponse);
          if (photoResponse?.success && photoResponse?.data) {
            // API returns data:image/jpeg;base64,... format - use sanitizer
            this.userProfileImage = this.sanitizer.bypassSecurityTrustUrl(photoResponse.data);
            this.userProfile.profileImageBase64 = photoResponse.data;
            console.log('Dashboard - Profile image set');
          }
        } catch (photoError) {
          console.warn('Could not fetch profile photo:', photoError);
          this.userProfileImage = this.IMAGE_PATHS.USER_PROFILE;
        }
      } else {
        this.userProfileImage = this.IMAGE_PATHS.USER_PROFILE;
      }

      // Set default values if profile is missing some fields
      if (this.userProfile) {
        this.userProfile.applications = this.userProfile.applications || [];
        this.userProfile.appName = this.userProfile.appName || 'No applications yet';
        this.userProfile.creationDateTime = this.userProfile.createdDateTime || this.userProfile.creationDateTime || new Date();
      }

      if (this.userProfile && this.userProfile.applications) {
        this.unverifiedApplications = this.userProfile.applications.filter((app: any) => app.active === 1 && !app.isVerified);
      }

      // Use individual dashboard data for user stats (right side & dropdown)
      const allDashboardData = results.dashboard?.resultData || [];
      // Use global dashboard data for graphs (no userId filter)
      const globalDashboardData = results.globalDashboard?.resultData || [];
      
      // Get top applications by sorting global dashboard data and enriching with full details
      const globalApps = (results.globalApplications?.data || []);
      const appMap = new Map(globalApps.map((app: any) => [app.id, app]));
      
      // Sort global dashboard by totalSentEmail and take top 10
      const top10ByUsage = globalDashboardData
        .filter((x: any) => x.appId !== 0 && x.totalSentEmail > 0)
        .sort((a: any, b: any) => (b.totalSentEmail || 0) - (a.totalSentEmail || 0))
        .slice(0, 10)
        .map((dashItem: any) => {
          const appDetails = appMap.get(dashItem.appId) || {};
          return {
            ...appDetails,
            totalSentEmail: dashItem.totalSentEmail
          };
        });
      this.top10Apps = top10ByUsage;
      
      // Keep individual data for stats and dropdown
      this.emailCount = [...allDashboardData.filter((x: any) => x.appId !== 0)];
      // Store global data separately for graphs
      this.globalChartData = [...globalDashboardData.filter((x: any) => x.appId !== 0)];
      // Use server totalRecords when available; fall back to current list length
      this.totalApplications = results.applications?.totalRecords
        ?? results.applications?.data?.length
        ?? this.appList.length;

      // Add "ALL" option - always present (individual user totals)
      this.emailCount.push({
        appId: 0,
        appName: DASHBOARD_LABELS.ALL_APPLICATIONS,
        todayEmail: this.emailCount.reduce((sum, count) => sum + (count.todayEmail || 0), 0),
        lastSevenDaysEmail: this.emailCount.reduce((sum, count) => sum + (count.lastSevenDaysEmail || 0), 0),
        lastThirtyDaysEmail: this.emailCount.reduce((sum, count) => sum + (count.lastThirtyDaysEmail || 0), 0),
        monthlyEmail: this.emailCount.reduce((sum, count) => sum + (count.monthlyEmail || 0), 0),
        yearlyEmail: this.emailCount.reduce((sum, count) => sum + (count.yearlyEmail || 0), 0),
        totalSentEmail: this.emailCount.reduce((sum, count) => sum + (count.totalSentEmail || 0), 0),
      });

      this.setUserTotalsFromEmailCount();
      // Use setTimeout to allow Angular to render the content before accessing the canvas
      setTimeout(() => this.initChart(), 150);
    } catch (err) {
      console.error('Error loading dashboard data:', err);
      // Set minimal profile so UI renders
      this.userProfile = {
        email: 'User',
        appName: 'No applications',
        applications: [],
        creationDateTime: new Date()
      };
      this.userProfileImage = this.IMAGE_PATHS.USER_PROFILE;
      this.emailCount = [{
        appId: 0,
        appName: DASHBOARD_LABELS.ALL_APPLICATIONS,
        todayEmail: 0,
        lastSevenDaysEmail: 0,
        lastThirtyDaysEmail: 0,
        monthlyEmail: 0,
        yearlyEmail: 0,
        totalSentEmail: 0,
      }];
      this.setUserTotalsFromEmailCount();
      setTimeout(() => this.initChart(), 150);
    }
  }

  onchangeApp(appIds: any) {
    // Selection only controls global charts; user stats remain individual
    setTimeout(() => this.initChart(), 0);
  }

  private setUserTotalsFromEmailCount() {
    const all = this.emailCount.find(x => x.appId === 0) || {
      todayEmail: 0,
      lastSevenDaysEmail: 0,
      lastThirtyDaysEmail: 0,
      monthlyEmail: 0,
      yearlyEmail: 0,
      totalSentEmail: 0
    };

    this.totalSentEmail = all.totalSentEmail || 0;
    this.todayAllEmail = all.todayEmail || 0;
    this.monthlyEmail = all.monthlyEmail || 0;
    this.lastSevenDaysEmail = all.lastSevenDaysEmail || 0;
    this.lastThirtyDaysEmail = all.lastThirtyDaysEmail || 0;
    this.yearlyEmail = all.yearlyEmail || 0;
  }

  initChart() {
    const canvas = document.getElementById('myChart') as HTMLCanvasElement;
    if (!canvas) {
      return;
    }

    // Filter chart data by selected apps, or show all if none selected
    let chartData = this.globalChartData;
    if (this.selectedAppIds && this.selectedAppIds.length > 0) {
      chartData = this.globalChartData.filter(x => this.selectedAppIds.includes(x.appId));
    }
    
    const barCharData = chartData.sort((a, b) => a.appName.localeCompare(b.appName));
    const labels = barCharData.map(x => x.appName);
    const emails = barCharData.map(x => x.totalSentEmail);

    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    // Build consistent blue gradient palette
    const gradients = labels.map(() => {
      const g = ctx.createLinearGradient(0, 0, canvas.width, 0);
      g.addColorStop(0, 'rgba(78, 115, 223, 0.55)');
      g.addColorStop(1, 'rgba(78, 115, 223, 0.95)');
      return g;
    });

    if (this.chart) { this.chart.destroy(); }
    this.chart = new Chart(ctx, {
      type: 'bar',
      data: {
        labels,
        datasets: [{
          label: DASHBOARD_LABELS.BAR_LABEL_SENT_EMAIL,
          data: emails,
          backgroundColor: gradients,
          hoverBackgroundColor: 'rgba(78, 115, 223, 1)',
          borderRadius: 6,
          borderSkipped: false,
          barThickness: 16,
          maxBarThickness: 20
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        indexAxis: 'y',
        animation: {
          duration: 500,
          easing: 'easeOutQuart'
        },
        plugins: {
          tooltip: {
            backgroundColor: '#1e293b',
            titleColor: '#e2e8f0',
            bodyColor: '#fff',
            borderColor: 'rgba(148, 163, 184, 0.2)',
            borderWidth: 1,
            padding: { top: 10, bottom: 10, left: 14, right: 14 },
            cornerRadius: 8,
            titleFont: { size: 11, weight: 'normal' },
            bodyFont: { size: 13, weight: 'bold' },
            displayColors: false,
            callbacks: {
              label: (context: any) => `${context.parsed.x.toLocaleString()} emails`
            }
          },
          legend: {
            display: false
          }
        },
        scales: {
          x: {
            border: { display: false },
            grid: { color: 'rgba(0, 0, 0, 0.04)', drawTicks: false },
            ticks: {
              color: '#94a3b8',
              font: { size: 10 },
              padding: 4,
              maxTicksLimit: 6,
              callback: function(value: any) {
                if (value >= 1000) return (value / 1000).toFixed(0) + 'k';
                return value;
              }
            }
          },
          y: {
            border: { display: false },
            grid: { display: false },
            ticks: {
              color: '#475569',
              font: { size: 11, weight: '500' },
              padding: 4
            }
          }
        }
      }
    });

    // Build breakdown doughnut for selected apps (aggregate if multiple)
    const breakdownCanvas = document.getElementById('breakdownChartUser') as HTMLCanvasElement;
    if (breakdownCanvas) {
      const bctx = breakdownCanvas.getContext('2d');
      if (bctx) {
        // Aggregate data from all selected apps, or from all apps if none selected
        let aggToday = 0;
        let agg7Days = 0;
        let agg30Days = 0;
        let aggMonthly = 0;
        let aggYearly = 0;
        
        // If no apps selected, use all global data. Otherwise use selected apps.
        const appsToAggregate = (!this.selectedAppIds || this.selectedAppIds.length === 0) 
          ? this.globalChartData 
          : this.globalChartData.filter(x => this.selectedAppIds.includes(x.appId));
        
        appsToAggregate.forEach(app => {
          aggToday += app.todayEmail || 0;
          agg7Days += app.lastSevenDaysEmail || 0;
          agg30Days += app.lastThirtyDaysEmail || 0;
          aggMonthly += app.monthlyEmail || 0;
          aggYearly += app.yearlyEmail || 0;
        });
        
        const labelsPie = ['Today', '7 Days', '30 Days', 'Monthly', 'Yearly'];
        const dataPie = [aggToday, agg7Days, agg30Days, aggMonthly, aggYearly];
        
        if (this.pieChart) { this.pieChart.destroy(); }

        // Create gradient for line chart
        const gradient = bctx.createLinearGradient(0, 0, 0, breakdownCanvas.height);
        gradient.addColorStop(0, 'rgba(78, 115, 223, 0.3)');
        gradient.addColorStop(1, 'rgba(78, 115, 223, 0.01)');

        // Create gradient for bar chart
        const barGradient = bctx.createLinearGradient(0, 0, 0, breakdownCanvas.height);
        barGradient.addColorStop(0, 'rgba(78, 115, 223, 0.95)');
        barGradient.addColorStop(1, 'rgba(78, 115, 223, 0.55)');

        this.pieChart = new Chart(bctx, {
          type: this.chartType,
          data: {
            labels: labelsPie,
            datasets: [{
              label: 'Emails Sent',
              data: dataPie,
              backgroundColor: this.chartType === 'line' ? gradient : barGradient,
              borderColor: 'rgba(78, 115, 223, 1)',
              borderWidth: this.chartType === 'line' ? 2.5 : 0,
              borderRadius: this.chartType === 'bar' ? 6 : 0,
              borderSkipped: false,
              fill: this.chartType === 'line',
              tension: 0.35,
              pointRadius: this.chartType === 'line' ? 4 : 0,
              pointHoverRadius: this.chartType === 'line' ? 6 : 0,
              pointBackgroundColor: 'rgba(78, 115, 223, 1)',
              pointBorderColor: '#fff',
              pointBorderWidth: 2,
              barPercentage: 0.65,
              categoryPercentage: 0.7
            }]
          },
          options: {
            responsive: true,
            maintainAspectRatio: false,
            animation: {
              duration: 500,
              easing: 'easeOutQuart'
            },
            plugins: {
              legend: { display: false },
              tooltip: {
                backgroundColor: '#1e293b',
                titleColor: '#e2e8f0',
                bodyColor: '#fff',
                borderColor: 'rgba(148, 163, 184, 0.2)',
                borderWidth: 1,
                padding: { top: 10, bottom: 10, left: 14, right: 14 },
                cornerRadius: 8,
                titleFont: { size: 11, weight: 'normal' },
                bodyFont: { size: 13, weight: 'bold' },
                displayColors: false,
                callbacks: {
                  title: (items: any) => items[0].label,
                  label: (context: any) => `${context.parsed.y.toLocaleString()} emails`
                }
              }
            },
            scales: {
              x: {
                border: { display: false },
                grid: { display: false },
                ticks: {
                  color: '#94a3b8',
                  font: { size: 10, weight: '500' },
                  padding: 4,
                  maxRotation: 0,
                  autoSkip: false
                }
              },
              y: {
                border: { display: false },
                beginAtZero: true,
                grid: {
                  color: 'rgba(0, 0, 0, 0.04)',
                  drawTicks: false
                },
                ticks: {
                  color: '#94a3b8',
                  font: { size: 10 },
                  padding: 8,
                  maxTicksLimit: 5,
                  callback: function(value: any) {
                    if (value >= 1000) return (value / 1000).toFixed(0) + 'k';
                    return value;
                  }
                }
              }
            }
          }
        });
      }
    }
  }
}